#!/bin/bash

# Documentation generation script for .NET projects
# 
# This script performs the following operations:
# 1. Cleans previous build artifacts
# 2. Generates documentation using DocFX (with optional server mode)
#
# Features:
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

Generate documentation for .NET projects using DocFX.

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
}

# Clean output directories before building
clean_output_directories() {
    print_status "Cleaning output directories..."
    
    # Remove the entire site directory
    if [[ -d "_site" ]]; then
        rm -rf _site
        print_success "Removed _site directory"
    fi
    
    # Clean API directory but preserve index.md and toc.yml
    if [[ -d "api" ]]; then
        find api -type f \( \( -name '*.yml' -a ! -name 'toc.yml' \) -o -name '.manifest' \) -delete 2>/dev/null || true
        print_success "Removed .yml and .manifest files from api directory (preserved index.md and toc.yml)"
    fi
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
    print_status "Generating documentation with DocFX..."
    
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
    
    # Generate documentation with DocFX
    generate_documentation
    
    print_success "All operations completed successfully!"
}

# Execute main function with all script arguments
main "$@"
