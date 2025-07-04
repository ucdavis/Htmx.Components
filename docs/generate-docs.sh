#!/bin/bash

# Documentation generation script for .NET projects
# 
# This script performs the following operations:
# 1. Cleans previous build artifacts
# 2. Discovers types from C# source code
# 3. Converts type mentions in markdown to API documentation links  
# 4. Generates documentation using DocFX (with optional server mode)
#
# Features:
# - Automatic type discovery from source code
# - Smart type link conversion with backup/restore
# - Robust process cleanup for server mode
# - Cross-platform compatibility (tested on macOS)

set -euo pipefail

# Configuration constants
readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly DEFAULT_PORT=8080
readonly DOCFX_CONFIG="docfx.json"

# Colors for output
readonly RED='\033[0;31m'
readonly GREEN='\033[0;32m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly NC='\033[0m' # No Color

# Cross-platform compatibility for sed command
# Some platforms use different sed syntax for in-place editing
safe_sed() {
    local pattern="$1"
    local file="$2"
    
    if sed --version >/dev/null 2>&1; then
        # GNU sed (Linux)
        sed -i "$pattern" "$file"
    else
        # BSD sed (macOS) 
        sed -i '' "$pattern" "$file"
    fi
}

# Logging functions
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Display usage information
usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Generate documentation for .NET projects using DocFX with automatic type link conversion.

Options:
  -s, --serve [PORT]    Serve documentation after building (default port: ${DEFAULT_PORT})
  -v, --verbose         Enable verbose output for debugging
  -m, --metadata-only   Generate only API metadata (skip site generation)
  -h, --help            Show this help message

Examples:
  $0                    Generate documentation
  $0 --serve            Generate and serve documentation on port ${DEFAULT_PORT}
  $0 --serve 8081       Generate and serve on port 8081
  $0 --verbose          Generate with verbose output
  $0 --metadata-only    Generate only API metadata files

For more information, see the project documentation.
EOF
}

# Parse command line arguments
parse_arguments() {
    SERVE=false
    PORT=$DEFAULT_PORT
    VERBOSE=false
    METADATA_ONLY=false

    while [[ $# -gt 0 ]]; do
        case $1 in
            -s|--serve)
                SERVE=true
                if [[ ${2:-} =~ ^[0-9]+$ ]]; then
                    PORT=$2
                    shift
                fi
                shift
                ;;
            -v|--verbose)
                VERBOSE=true
                shift
                ;;
            -m|--metadata-only)
                METADATA_ONLY=true
                shift
                ;;
            -h|--help)
                usage
                exit 0
                ;;
            *)
                print_error "Unknown option: $1"
                usage
                exit 1
                ;;
        esac
    done
}

# Validate environment and prerequisites
validate_environment() {
    cd "$SCRIPT_DIR"
    
    print_status "Starting documentation generation..."
    print_status "Working directory: $SCRIPT_DIR"
    
    if [[ ! -f "$DOCFX_CONFIG" ]]; then
        print_error "$DOCFX_CONFIG not found in current directory"
        exit 1
    fi
    
    if ! command -v docfx &> /dev/null; then
        print_error "DocFX is not installed or not in PATH"
        print_error "Please install DocFX: https://dotnet.github.io/docfx/"
        exit 1
    fi
    
    # Check for perl availability (used for advanced generic type processing)
    if ! command -v perl &> /dev/null; then
        print_warning "Perl not found - generic type link conversion will be limited"
        print_warning "For Windows users: Consider installing Perl or using WSL for full functionality"
    fi
}

# Clean output directories before building
clean_output_directories() {
    print_status "Cleaning output directories..."
    
    # Remove the entire site directory
    if [[ -d "_site" ]]; then
        rm -rf _site
        print_success "Removed _site directory"
    fi
    
    # Clean API directory but preserve index.md
    if [[ -d "api" ]]; then
        find api -type f \( -name '*.yml' -o -name '.manifest' \) -delete
        print_success "Removed .yml and .manifest files from api directory (preserved index.md)"
    fi
}

# ============================================================================
# TYPE DISCOVERY AND LINK CONVERSION FUNCTIONS
# ============================================================================

# Discover C# types from source files
discover_types() {
    local src_dir="$1"
    [[ "$VERBOSE" == true ]] && print_status "Discovering types from source code in: $src_dir"
    
    # Create a temporary file to store results
    local temp_file=$(mktemp)
    
    # Find all C# files and process them
    find "$src_dir" -name "*.cs" -type f > "${temp_file}.files" 2>/dev/null || true
    
    if [[ ! -s "${temp_file}.files" ]]; then
        [[ "$VERBOSE" == true ]] && print_warning "No .cs files found in $src_dir"
        rm -f "${temp_file}.files" "${temp_file}"
        return 1
    fi
    
    while IFS= read -r file; do
        [[ ! -f "$file" ]] && continue
        
        # Extract namespace from the file
        local namespace
        namespace=$(grep "^namespace " "$file" 2>/dev/null | head -1 | sed 's/namespace //' | sed 's/[;{].*//' || true)
        
        if [[ -n "$namespace" ]]; then
            # Find type declarations and extract type names
            grep -E "^[[:space:]]*(public|internal) (class|interface|enum|struct) " "$file" 2>/dev/null | while IFS= read -r line; do
                local type_name
                type_name=$(echo "$line" | sed -E 's/.*(public|internal) (class|interface|enum|struct) ([A-Za-z_][A-Za-z0-9_]*).*/\3/' 2>/dev/null || true)
                
                if [[ -n "$type_name" && "$type_name" != "$line" ]]; then
                    echo "${type_name}:${namespace}"
                fi
            done >> "$temp_file"
        fi
    done < "${temp_file}.files"
    
    # Output unique results
    if [[ -s "$temp_file" ]]; then
        sort -u "$temp_file"
    fi
    
    # Cleanup
    rm -f "${temp_file}.files" "$temp_file"
}

# Find the source directory containing C# files
find_source_directory() {
    local script_dir="$1"
    local candidates=("$script_dir/../src" "$script_dir/..")
    
    [[ "$VERBOSE" == true ]] && print_status "Looking for source directory from: $script_dir"
    
    for dir in "${candidates[@]}"; do
        [[ "$VERBOSE" == true ]] && print_status "Checking candidate directory: $dir"
        
        if [[ -d "$dir" ]]; then
            [[ "$VERBOSE" == true ]] && print_status "Directory exists: $dir"
            
            # Count C# files
            local cs_count
            cs_count=$(find "$dir" -name "*.cs" -type f 2>/dev/null | wc -l)
            [[ "$VERBOSE" == true ]] && print_status "Found $cs_count C# files in $dir"
            
            if [[ $cs_count -gt 0 ]]; then
                echo "$dir"
                return 0
            fi
        else
            [[ "$VERBOSE" == true ]] && print_status "Directory does not exist: $dir"
        fi
    done
    
    print_error "Could not find C# source files. Tried: ${candidates[*]}"
    
    # Debug: show what's actually in the script directory
    if [[ "$VERBOSE" == true ]]; then
        print_status "Contents of script directory ($script_dir):"
        ls -la "$script_dir" || true
        print_status "Contents of parent directory ($script_dir/..):"
        ls -la "$script_dir/.." || true
    fi
    
    return 1
}

# Build type mappings from discovered types
build_type_mappings() {
    local script_dir="$1"
    local src_dir
    
    if ! src_dir=$(find_source_directory "$script_dir"); then
        return 1
    fi
    
    print_status "Searching for types in: $src_dir"
    
    # Check if source directory actually contains C# files
    if ! find "$src_dir" -name "*.cs" -type f | head -1 >/dev/null 2>&1; then
        print_warning "No C# files found in source directory: $src_dir"
        return 1
    fi
    
    local mappings=()
    local types_output
    types_output=$(discover_types "$src_dir")
    
    if [[ -n "$types_output" ]]; then
        while IFS= read -r line; do
            [[ -n "$line" ]] && mappings+=("$line")
        done <<< "$types_output"
    fi
    
    print_status "Found ${#mappings[@]} types"
    
    if [[ ${#mappings[@]} -gt 0 ]]; then
        printf '%s\n' "${mappings[@]}"
    else
        [[ "$VERBOSE" == true ]] && print_warning "No types discovered from source code"
        return 1
    fi
}

# Convert type links in a single markdown file
convert_type_links_in_file() {
    local file="$1"
    shift
    local mappings=("$@")
    
    [[ "$VERBOSE" == true ]] && print_status "Processing: $file"
    
    # Validate file exists and is readable
    if [[ ! -f "$file" ]]; then
        [[ "$VERBOSE" == true ]] && print_warning "File not found: $file"
        return 1
    fi
    
    if [[ ! -r "$file" ]]; then
        [[ "$VERBOSE" == true ]] && print_warning "File not readable: $file"
        return 1
    fi
    
    # Create backup for safety
    if ! cp "$file" "${file}.bak"; then
        [[ "$VERBOSE" == true ]] && print_warning "Failed to create backup for: $file"
        return 1
    fi
    
    # Process each type mapping
    for mapping in "${mappings[@]}"; do
        local type_name namespace base_api_url
        IFS=':' read -r type_name namespace <<< "$mapping"
        
        [[ -z "$type_name" || -z "$namespace" ]] && continue
        
        base_api_url="../../api/${namespace}.${type_name}.html"
        
        # Convert simple type references (e.g., `TypeName` -> [`TypeName`](url))
        if grep -qF "\`${type_name}\`" "$file" 2>/dev/null && ! grep -qF "[\`${type_name}\`](" "$file" 2>/dev/null; then
            if ! safe_sed "s|\`${type_name}\`|[\`${type_name}\`](${base_api_url})|g" "$file"; then
                [[ "$VERBOSE" == true ]] && print_warning "Failed to process simple type: $type_name in $file"
            fi
        fi
        
        # Convert generic type references for specific patterns
        if [[ "$type_name" =~ ^(ModelHandler|TableModel|TableColumnModel|.*Builder)$ ]]; then
            if grep -qF "\`${type_name}<" "$file" 2>/dev/null && ! grep -qF "[\`${type_name}<" "$file" 2>/dev/null; then
                if command -v perl >/dev/null 2>&1; then
                    if ! perl -i -pe "
                        s|\`${type_name}<([^>]+)>\`|
                            my \$type_params = \$1;
                            my \$param_count = (\$type_params =~ tr/,/,/) + 1;
                            \"[\\\`${type_name}<\$type_params>\\\`](../../api/${namespace}.${type_name}-\$param_count.html)\"
                        |gex" "$file" 2>/dev/null; then
                        [[ "$VERBOSE" == true ]] && print_warning "Failed to process generic type: $type_name in $file"
                    fi
                else
                    [[ "$VERBOSE" == true ]] && print_warning "Perl not available - skipping generic type processing for $type_name"
                fi
            fi
        fi
    done
    
    # Check if file was modified and provide feedback
    if ! cmp -s "$file" "${file}.bak" 2>/dev/null; then
        [[ "$VERBOSE" == true ]] && print_success "  ✓ Updated: $file"
        rm -f "${file}.bak" 2>/dev/null || true
        return 0
    else
        [[ "$VERBOSE" == true ]] && print_status "  - No changes needed: $file"
        mv "${file}.bak" "$file" 2>/dev/null || cp "${file}.bak" "$file"
        rm -f "${file}.bak" 2>/dev/null || true
        return 1
    fi
}

# Process all markdown files for type link conversion
convert_type_links() {
    print_status "Step 1: Converting type mentions to API documentation links..."
    
    # Build dynamic type mappings from source code
    local mappings=()
    local mappings_output
    
    # Temporarily disable exit on error for type discovery
    set +e
    mappings_output=$(build_type_mappings "$SCRIPT_DIR")
    local discovery_result=$?
    set -e
    
    if [[ $discovery_result -eq 0 && -n "$mappings_output" ]]; then
        while IFS= read -r line; do
            [[ -n "$line" ]] && mappings+=("$line")
        done <<< "$mappings_output"
    fi
    
    if [[ ${#mappings[@]} -eq 0 ]]; then
        print_warning "No types found in source code - skipping type link conversion"
        print_status "Documentation will still be generated, but without automatic type linking"
        return 0
    fi
    
    print_status "Found ${#mappings[@]} types to process"
    
    # Process all markdown files (excluding API directory)
    local files_processed=0
    local files_modified=0
    
    # Find markdown files and process them
    local md_files=()
    while IFS= read -r -d '' file; do
        md_files+=("$file")
    done < <(find "$SCRIPT_DIR" -name "*.md" -type f ! -path "*/api/*" -print0)
    
    [[ "$VERBOSE" == true ]] && print_status "Found ${#md_files[@]} markdown files to process"
    
    # Debug: show the types we found
    if [[ "$VERBOSE" == true ]]; then
        print_status "Types found:"
        for mapping in "${mappings[@]}"; do
            echo "  - $mapping"
        done
    fi
    
    for file in "${md_files[@]}"; do
        if [[ -f "$file" ]]; then
            [[ "$VERBOSE" == true ]] && print_status "Processing file: $file"
            # Pass mappings as separate arguments, handle return code explicitly
            set +e  # Temporarily disable exit on error
            convert_type_links_in_file "$file" "${mappings[@]}"
            local result=$?
            set -e  # Re-enable exit on error
            
            if [[ $result -eq 0 ]]; then
                ((files_modified++))
                [[ "$VERBOSE" == true ]] && print_status "File was modified"
            else
                [[ "$VERBOSE" == true ]] && print_status "File was not modified or had issues"
            fi
            ((files_processed++))
        else
            [[ "$VERBOSE" == true ]] && print_warning "Skipping non-existent file: $file"
        fi
    done
    
    print_status "Processed $files_processed markdown files, modified $files_modified"
    print_success "Type link conversion completed"
    echo ""
}

# ============================================================================
# DOCFX OPERATIONS
# ============================================================================

# Build the DocFX command based on options
build_docfx_command() {
    local cmd="docfx $DOCFX_CONFIG"
    
    if [[ "$METADATA_ONLY" == true ]]; then
        cmd="docfx metadata $DOCFX_CONFIG"
    elif [[ "$SERVE" == true ]]; then
        cmd="docfx $DOCFX_CONFIG --serve --port $PORT"
    fi
    
    [[ "$VERBOSE" == true ]] && cmd="$cmd --logLevel Verbose"
    echo "$cmd"
}

# Cleanup function for DocFX server processes
cleanup_docfx_server() {
    local docfx_pid="$1"
    local port="$2"
    
    print_status "Stopping docfx server..."
    
    # Kill the specific process first
    kill "$docfx_pid" 2>/dev/null || true
    
    # Kill any remaining docfx server processes
    pkill -f "docfx.*--serve" 2>/dev/null || true
    
    # Kill any process using our port (try different approaches for cross-platform compatibility)
    if command -v lsof >/dev/null 2>&1; then
        # Unix/Linux/macOS with lsof
        lsof -ti:"$port" 2>/dev/null | xargs kill -9 2>/dev/null || true
    elif command -v netstat >/dev/null 2>&1; then
        # Windows/Git Bash with netstat
        local pid
        pid=$(netstat -ano | grep ":$port " | awk '{print $5}' | head -1)
        [[ -n "$pid" ]] && taskkill //PID "$pid" //F 2>/dev/null || true
    fi
    
    exit 0
}

# Generate documentation using DocFX
generate_documentation() {
    print_status "Step 2: Generating documentation with DocFX..."
    
    local docfx_cmd
    docfx_cmd=$(build_docfx_command)
    print_status "Running: $docfx_cmd"
    echo ""
    
    if [[ "$SERVE" == true && "$METADATA_ONLY" == false ]]; then
        # Start DocFX server in background
        eval "$docfx_cmd" &
        local docfx_pid=$!
        
        # Set up signal handler for graceful shutdown
        trap "cleanup_docfx_server $docfx_pid $PORT" INT
        
        # Wait a moment for server to start, then check if it's running
        sleep 3
        if kill -0 "$docfx_pid" 2>/dev/null; then
            print_success "Documentation server started successfully!"
            print_success "Open your browser to: http://localhost:$PORT"
            echo ""
            print_status "Press Ctrl+C to stop the server"
            wait "$docfx_pid"
        else
            print_error "Failed to start documentation server"
            exit 1
        fi
        
        # Clean up trap when done
        trap - INT
    else
        # Run DocFX build/metadata generation
        if eval "$docfx_cmd"; then
            echo ""
            if [[ "$METADATA_ONLY" == true ]]; then
                print_success "API metadata generation completed!"
                print_success "Metadata files generated in: api/"
            else
                print_success "Documentation generation completed!"
                print_success "Documentation generated in: _site/"
                print_status "To serve locally, run: docfx $DOCFX_CONFIG --serve"
            fi
        else
            print_error "Documentation generation failed"
            exit 1
        fi
    fi
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

main() {
    # Parse command line arguments
    parse_arguments "$@"
    
    # Validate environment and prerequisites
    validate_environment
    
    # Clean previous build artifacts
    clean_output_directories
    
    # Convert type mentions to API documentation links
    convert_type_links
    
    # Generate documentation with DocFX
    generate_documentation
    
    print_success "All operations completed successfully!"
}

# Execute main function with all script arguments
main "$@"
