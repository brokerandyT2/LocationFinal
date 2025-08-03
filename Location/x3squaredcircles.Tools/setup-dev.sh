#!/bin/bash
# setup-dev.sh - Location Development Environment Setup
# Single repository per vertical, Core dependencies via Azure Artifacts

set -euo pipefail

# Global variables
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOCATION_ROOT="$HOME/Location"
VERTICAL=""
TOOLS_MODE="artifacts"  # "artifacts" or "source"
UPDATE_MODE=false
UNINSTALL_MODE=false
DEBUG_MODE=false
DRY_RUN=false
PLATFORM=""
PACKAGE_MANAGER=""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" >&2
}

fatal() {
    log_error "$1"
    exit 1
}

# Platform detection
detect_platform() {
    case "$(uname -s)" in
        Darwin*)    PLATFORM="macos" ;;
        Linux*)     PLATFORM="linux" ;;
        MINGW*|MSYS*|CYGWIN*) PLATFORM="windows" ;;
        *) fatal "Unsupported platform: $(uname -s)" ;;
    esac
    
    log_info "Detected platform: $PLATFORM"
    
    # Detect package manager on Linux
    if [[ "$PLATFORM" == "linux" ]]; then
        if command -v apt-get >/dev/null; then
            PACKAGE_MANAGER="apt"
        elif command -v yum >/dev/null; then
            PACKAGE_MANAGER="yum"
        elif command -v pacman >/dev/null; then
            PACKAGE_MANAGER="pacman"
        else
            fatal "No supported package manager found (apt, yum, pacman)"
        fi
        log_info "Detected package manager: $PACKAGE_MANAGER"
    fi
}

# Command exists check
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Version comparison
version_gt() {
    test "$(printf '%s\n' "$@" | sort -V | head -n 1)" != "$1"
}

# Azure authentication
authenticate_azure() {
    log_info "Authenticating with Azure AD..."
    
    if ! az account show >/dev/null 2>&1; then
        log_info "Please sign in to Azure AD"
        az login --scope https://pkgs.dev.azure.com/.default
    fi
    
    local tenant_id=$(az account show --query tenantId -o tsv)
    local user_principal=$(az account show --query user.name -o tsv)
    
    log_info "Authenticated as: $user_principal"
    
    # Test Azure Artifacts access
    if ! az artifacts universal list \
        --organization https://dev.azure.com/x3squaredcircles \
        --feed x3squaredcircles-tools >/dev/null 2>&1; then
        fatal "No access to Azure Artifacts. Contact IT for permissions."
    fi
    
    log_success "Azure authentication successful"
}

# Install platform package manager
install_package_manager() {
    case $PLATFORM in
        "macos")
            if ! command_exists brew; then
                log_info "Installing Homebrew..."
                if [[ "$DRY_RUN" == false ]]; then
                    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
                fi
            else
                log_info "Homebrew already installed"
            fi
            ;;
        "windows")
            if ! command_exists choco; then
                log_info "Installing Chocolatey..."
                if [[ "$DRY_RUN" == false ]]; then
                    powershell -Command "Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))"
                fi
            else
                log_info "Chocolatey already installed"
            fi
            ;;
        "linux")
            # Package managers are built-in on Linux
            log_info "Using system package manager: $PACKAGE_MANAGER"
            ;;
    esac
}

# Install .NET 9 SDK
install_dotnet() {
    local required_version="9.0"
    local installed_version=""
    
    if command_exists dotnet; then
        installed_version=$(dotnet --version 2>/dev/null | cut -d. -f1-2 || echo "none")
    else
        installed_version="none"
    fi
    
    if [[ "$installed_version" == "$required_version"* ]]; then
        log_info ".NET $installed_version already installed"
        return 0
    fi
    
    log_info "Installing .NET $required_version SDK..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install .NET $required_version SDK"
        return 0
    fi
    
    case $PLATFORM in
        "macos")
            brew install --cask dotnet-sdk
            ;;
        "linux")
            case $PACKAGE_MANAGER in
                "apt")
                    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
                    sudo dpkg -i packages-microsoft-prod.deb
                    rm packages-microsoft-prod.deb
                    sudo apt-get update
                    sudo apt-get install -y dotnet-sdk-9.0
                    ;;
                "yum")
                    sudo rpm -Uvh https://packages.microsoft.com/config/fedora/37/packages-microsoft-prod.rpm
                    sudo yum install -y dotnet-sdk-9.0
                    ;;
            esac
            ;;
        "windows")
            choco install dotnet-9.0-sdk -y
            ;;
    esac
    
    # Verify installation
    if command_exists dotnet; then
        local new_version=$(dotnet --version)
        log_success ".NET SDK $new_version installed successfully"
    else
        fatal "Failed to install .NET SDK"
    fi
}

# Install Git
install_git() {
    if command_exists git; then
        log_info "Git already installed: $(git --version)"
        return 0
    fi
    
    log_info "Installing Git..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install Git"
        return 0
    fi
    
    case $PLATFORM in
        "macos")
            brew install git
            ;;
        "linux")
            case $PACKAGE_MANAGER in
                "apt")
                    sudo apt-get update
                    sudo apt-get install -y git
                    ;;
                "yum")
                    sudo yum install -y git
                    ;;
                "pacman")
                    sudo pacman -S --noconfirm git
                    ;;
            esac
            ;;
        "windows")
            choco install git -y
            ;;
    esac
    
    log_success "Git installed successfully"
}

# Install Minikube
install_minikube() {
    if command_exists minikube; then
        log_info "Minikube already installed: $(minikube version --short)"
        return 0
    fi
    
    log_info "Installing Minikube..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install Minikube"
        return 0
    fi
    
    case $PLATFORM in
        "macos")
            brew install minikube
            ;;
        "linux")
            curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
            sudo install minikube-linux-amd64 /usr/local/bin/minikube
            rm minikube-linux-amd64
            ;;
        "windows")
            choco install minikube -y
            ;;
    esac
    
    log_success "Minikube installed successfully"
}

# Install kubectl
install_kubectl() {
    if command_exists kubectl; then
        log_info "kubectl already installed: $(kubectl version --client --short 2>/dev/null || kubectl version --client)"
        return 0
    fi
    
    log_info "Installing kubectl..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install kubectl"
        return 0
    fi
    
    case $PLATFORM in
        "macos")
            brew install kubectl
            ;;
        "linux")
            case $PACKAGE_MANAGER in
                "apt")
                    sudo apt-get update
                    sudo apt-get install -y apt-transport-https ca-certificates curl
                    curl -fsSL https://pkgs.k8s.io/core:/stable:/v1.28/deb/Release.key | sudo gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg
                    echo 'deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/v1.28/deb/ /' | sudo tee /etc/apt/sources.list.d/kubernetes.list
                    sudo apt-get update
                    sudo apt-get install -y kubectl
                    ;;
                "yum")
                    cat <<EOF | sudo tee /etc/yum.repos.d/kubernetes.repo
[kubernetes]
name=Kubernetes
baseurl=https://pkgs.k8s.io/core:/stable:/v1.28/rpm/
enabled=1
gpgcheck=1
gpgkey=https://pkgs.k8s.io/core:/stable:/v1.28/rpm/repodata/repomd.xml.key
EOF
                    sudo yum install -y kubectl
                    ;;
            esac
            ;;
        "windows")
            choco install kubernetes-cli -y
            ;;
    esac
    
    log_success "kubectl installed successfully"
}

# Install Visual Studio Code
install_vscode() {
    if command_exists code; then
        log_info "VS Code already installed: $(code --version | head -n1)"
        return 0
    fi
    
    log_info "Installing Visual Studio Code..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install Visual Studio Code"
        return 0
    fi
    
    case $PLATFORM in
        "macos")
            brew install --cask visual-studio-code
            ;;
        "linux")
            case $PACKAGE_MANAGER in
                "apt")
                    wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > packages.microsoft.gpg
                    sudo install -o root -g root -m 644 packages.microsoft.gpg /etc/apt/trusted.gpg.d/
                    sudo sh -c 'echo "deb [arch=amd64,arm64,armhf signed-by=/etc/apt/trusted.gpg.d/packages.microsoft.gpg] https://packages.microsoft.com/repos/code stable main" > /etc/apt/sources.list.d/vscode.list'
                    sudo apt-get update
                    sudo apt-get install -y code
                    ;;
            esac
            ;;
        "windows")
            choco install vscode -y
            ;;
    esac
    
    log_success "VS Code installed successfully"
}

# Install VS Code extensions
install_vscode_extensions() {
    if ! command_exists code; then
        log_warning "VS Code not found, skipping extension installation"
        return 0
    fi
    
    log_info "Installing VS Code extensions..."
    
    local extensions=(
        "ms-dotnettools.csharp"
        "ms-dotnettools.vscode-dotnet-runtime"
        "ms-vscode.azure-account"
        "eamodio.gitlens"
        "rangav.vscode-thunder-client"
        "ms-azuretools.vscode-docker"
    )
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install VS Code extensions: ${extensions[*]}"
        return 0
    fi
    
    for extension in "${extensions[@]}"; do
        log_info "Installing extension: $extension"
        code --install-extension "$extension" --force
    done
    
    log_success "VS Code extensions installed successfully"
}

# Install Azure CLI
install_azure_cli() {
    if command_exists az; then
        log_info "Azure CLI already installed: $(az --version | head -n1 | cut -d' ' -f2)"
        return 0
    fi
    
    log_info "Installing Azure CLI..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install Azure CLI"
        return 0
    fi
    
    case $PLATFORM in
        "macos")
            brew install azure-cli
            ;;
        "linux")
            case $PACKAGE_MANAGER in
                "apt")
                    curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
                    ;;
                "yum")
                    sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
                    sudo sh -c 'echo -e "[azure-cli]\nname=Azure CLI\nbaseurl=https://packages.microsoft.com/yumrepos/azure-cli\nenabled=1\ngpgcheck=1\ngpgkey=https://packages.microsoft.com/keys/microsoft.asc" > /etc/yum.repos.d/azure-cli.repo'
                    sudo yum install -y azure-cli
                    ;;
            esac
            ;;
        "windows")
            choco install azure-cli -y
            ;;
    esac
    
    log_success "Azure CLI installed successfully"
}

# Start Minikube cluster
start_minikube() {
    log_info "Starting Minikube cluster..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would start Minikube cluster"
        return 0
    fi
    
    # Check if Minikube is already running
    if minikube status --profile=location-dev >/dev/null 2>&1; then
        log_info "Minikube cluster already running"
        return 0
    fi
    
    # Start Minikube with recommended settings for development
    log_info "Starting Minikube cluster (this may take a few minutes)..."
    minikube start \
        --cpus=4 \
        --memory=8192 \
        --disk-size=20gb \
        --kubernetes-version=v1.28.0 \
        --driver=docker \
        --profile=location-dev
    
    # Enable necessary addons
    log_info "Enabling Minikube addons..."
    minikube addons enable ingress --profile=location-dev
    minikube addons enable dashboard --profile=location-dev
    minikube addons enable metrics-server --profile=location-dev
    
    # Set kubectl context
    kubectl config use-context location-dev
    
    log_success "Minikube cluster started successfully"
}

# Setup development database in Kubernetes
setup_database() {
    log_info "Setting up development database in Kubernetes..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would setup SQL Server in Kubernetes and DataConsumption database"
        return 0
    fi
    
    # Ensure Minikube is running
    if ! minikube status --profile=location-dev >/dev/null 2>&1; then
        log_info "Minikube not running, starting cluster..."
        start_minikube
    fi
    
    # Create namespace for Location development
    kubectl create namespace location-dev --dry-run=client -o yaml | kubectl apply -f -
    
    # Apply SQL Server Kubernetes manifests
    log_info "Creating SQL Server deployment..."
    
    # Check if manifests directory exists
    local manifests_dir="$SCRIPT_DIR/k8s"
    if [[ ! -d "$manifests_dir" ]]; then
        fatal "Kubernetes manifests directory not found: $manifests_dir"
    fi
    
    # Apply manifests in order
    kubectl apply -f "$manifests_dir/namespace.yaml"
    kubectl apply -f "$manifests_dir/secret.yaml"
    kubectl apply -f "$manifests_dir/pvc.yaml"
    kubectl apply -f "$manifests_dir/deployment.yaml"
    kubectl apply -f "$manifests_dir/service.yaml"
    
    # Wait for SQL Server pod to be ready
    log_info "Waiting for SQL Server to be ready..."
    kubectl wait --for=condition=ready pod -l app=mssql -n location-dev --timeout=300s
    
    # Get the NodePort for SQL Server
    local nodeport=$(kubectl get service mssql-service -n location-dev -o jsonpath='{.spec.ports[0].nodePort}')
    local minikube_ip=$(minikube ip --profile=location-dev)
    
    log_info "SQL Server accessible at: $minikube_ip:$nodeport"
    
    # Wait a bit more for SQL Server to fully start
    sleep 30
    
    # Create DataConsumption database
    log_info "Creating DataConsumption database..."
    local pod_name=$(kubectl get pods -n location-dev -l app=mssql -o jsonpath='{.items[0].metadata.name}')
    
    kubectl exec -n location-dev "$pod_name" -- /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa -P "LocationDev2024!" \
        -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'DataConsumption') CREATE DATABASE DataConsumption"
    
    # Create connection string file for easy reference
    mkdir -p "$LOCATION_ROOT"
    cat > "$LOCATION_ROOT/database-connection.txt" << EOF
SQL Server Connection Details:
Host: $minikube_ip
Port: $nodeport
Database: DataConsumption
Username: sa
Password: LocationDev2024!

Connection String:
Server=$minikube_ip,$nodeport;Database=DataConsumption;User Id=sa;Password=LocationDev2024!;TrustServerCertificate=true;
EOF
    
    log_success "Development database ready in Kubernetes"
    log_info "Connection details saved to: $LOCATION_ROOT/database-connection.txt"
}

# Clone repository
clone_repository() {
    local vertical="$1"
    local repo_dir="$LOCATION_ROOT/Location-$vertical"
    
    if [[ -d "$repo_dir" ]]; then
        log_info "Repository Location-$vertical already exists"
        if [[ "$UPDATE_MODE" == true ]]; then
            log_info "Updating repository..."
            cd "$repo_dir"
            git pull
        fi
        return 0
    fi
    
    log_info "Cloning Location-$vertical repository..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would clone Location-$vertical repository"
        return 0
    fi
    
    mkdir -p "$LOCATION_ROOT"
    cd "$LOCATION_ROOT"
    
    git clone "https://dev.azure.com/x3squaredcircles/Location/_git/Location-$vertical"
    
    log_success "Repository cloned successfully"
}

# Configure Git for repository
configure_git() {
    local vertical="$1"
    local repo_dir="$LOCATION_ROOT/Location-$vertical"
    
    if [[ ! -d "$repo_dir" ]]; then
        log_warning "Repository directory not found, skipping Git configuration"
        return 0
    fi
    
    cd "$repo_dir"
    
    log_info "Configuring Git repository..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would configure Git repository"
        return 0
    fi
    
    # Setup .gitignore if it doesn't exist
    if [[ ! -f ".gitignore" ]]; then
        cat > .gitignore << 'EOF'
# Build Artifacts
**/bin/
**/obj/
*.dll
*.pdb
*.exe
!*.exe.config

# Generated Mobile Adapters (NEVER commit)
**/generated/
**/AndroidUI/generated/
**/iOSUI/generated/
**/*Adapter.kt
**/*Adapter.swift

# Tool Outputs
**/tools/bin/
**/tools/obj/

# Azure Artifacts Cache
**/.nuget/
**/packages/

# IDE
.vs/
.vscode/settings.json
*.user
*.suo

# OS
.DS_Store
Thumbs.db
EOF
        git add .gitignore
        git commit -m "Add standard .gitignore for Location project"
    fi
    
    # Setup nuget.config for Azure Artifacts
    if [[ ! -f "nuget.config" ]]; then
        cat > nuget.config << 'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="Azure Artifacts - Core" value="https://pkgs.dev.azure.com/x3squaredcircles/_packaging/LocationLibraries-Core/nuget/v3/index.json" />
    <add key="Azure Artifacts - Tools" value="https://pkgs.dev.azure.com/x3squaredcircles/_packaging/x3squaredcircles-tools/nuget/v3/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <Azure_Artifacts_-_Core>
      <add key="Username" value="AzureDevOps" />
      <add key="ClearTextPassword" value="%AZURE_ARTIFACTS_TOKEN%" />
    </Azure_Artifacts_-_Core>
    <Azure_Artifacts_-_Tools>
      <add key="Username" value="AzureDevOps" />
      <add key="ClearTextPassword" value="%AZURE_ARTIFACTS_TOKEN%" />
    </Azure_Artifacts_-_Tools>
  </packageSourceCredentials>
</configuration>
EOF
        git add nuget.config
        git commit -m "Add Azure Artifacts package configuration"
    fi
    
    log_success "Git repository configured"
}

# Install Location tools from Azure Artifacts
install_location_tools() {
    if [[ "$TOOLS_MODE" == "source" ]]; then
        install_tools_from_source
    else
        install_tools_from_artifacts
    fi
}

# Install tools from Azure Artifacts
install_tools_from_artifacts() {
    log_info "Installing Location tools from Azure Artifacts..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install Location tools from Azure Artifacts"
        return 0
    fi
    
    # Setup credential provider for Azure Artifacts
    if ! command_exists dotnet; then
        fatal ".NET SDK must be installed before installing tools"
    fi
    
    # Install Azure Artifacts Credential Provider
    if [[ ! -f "$HOME/.nuget/plugins/netcore/CredentialProvider.Microsoft/CredentialProvider.Microsoft.dll" ]]; then
        log_info "Installing Azure Artifacts Credential Provider..."
        case $PLATFORM in
            "windows")
                iex "& { $(irm https://aka.ms/install-artifacts-credprovider.ps1) }"
                ;;
            *)
                sh -c "$(curl -fsSL https://aka.ms/install-artifacts-credprovider.sh)"
                ;;
        esac
    fi
    
    # Install Location tools
    local tools=(
        "location-version-calculator"
        "sql-schema-generator"
        "location-api-generator"
        "photography-adapter-generator"
        "location-pr-enhancer"
    )
    
    for tool in "${tools[@]}"; do
        log_info "Installing $tool..."
        dotnet tool install -g "$tool" \
            --add-source https://pkgs.dev.azure.com/x3squaredcircles/_packaging/x3squaredcircles-tools/nuget/v3/index.json \
            --interactive 2>/dev/null || \
        dotnet tool update -g "$tool" \
            --add-source https://pkgs.dev.azure.com/x3squaredcircles/_packaging/x3squaredcircles-tools/nuget/v3/index.json \
            --interactive 2>/dev/null || true
    done
    
    log_success "Location tools installed successfully"
}

# Install tools from source (for tool developers)
install_tools_from_source() {
    log_info "Installing Location tools from source..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would build and install Location tools from source"
        return 0
    fi
    
    local tools_dir="$LOCATION_ROOT/Location-Tools"
    
    # Clone tools repository if it doesn't exist
    if [[ ! -d "$tools_dir" ]]; then
        log_info "Cloning Location-Tools repository..."
        cd "$LOCATION_ROOT"
        git clone "https://dev.azure.com/x3squaredcircles/Location/_git/Location-Tools"
    else
        log_info "Updating Location-Tools repository..."
        cd "$tools_dir"
        git pull
    fi
    
    cd "$tools_dir"
    
    # Build all tools
    log_info "Building tools from source..."
    dotnet build --configuration Release
    
    # Install tools locally
    local tool_projects=(
        "src/LocationVersionCalculator"
        "src/SQLSchemaGenerator" 
        "src/LocationAPIGenerator"
        "src/PhotographyAdapterGenerator"
        "src/LocationPREnhancer"
    )
    
    for project in "${tool_projects[@]}"; do
        if [[ -d "$project" ]]; then
            local tool_name=$(basename "$project" | tr '[:upper:]' '[:lower:]')
            log_info "Installing $tool_name from source..."
            dotnet tool install -g --add-source "$PWD/$project/bin/Release" "$tool_name" || \
            dotnet tool update -g --add-source "$PWD/$project/bin/Release" "$tool_name" || true
        fi
    done
    
    log_success "Location tools built and installed from source"
}

# Setup VS Code build integration
setup_vscode_build_integration() {
    local vertical="$1"
    local repo_dir="$LOCATION_ROOT/Location-$vertical"
    local vscode_dir="$repo_dir/.vscode"
    
    if [[ ! -d "$repo_dir" ]]; then
        log_warning "Repository directory not found, skipping VS Code build integration"
        return 0
    fi
    
    log_info "Setting up VS Code build integration..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would setup VS Code build integration"
        return 0
    fi
    
    mkdir -p "$vscode_dir"
    
    # Create tasks.json for build integration
    cat > "$vscode_dir/tasks.json" << EOF
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "type": "shell",
            "command": "dotnet",
            "args": ["build"],
            "group": {
                "kind": "build",
                "isDefault": true
            },
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": false,
                "panel": "shared"
            },
            "problemMatcher": "\$msCompile"
        },
        {
            "label": "build with tools",
            "type": "shell",
            "command": "dotnet",
            "args": ["build", "--verbosity", "normal"],
            "group": "build",
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": false,
                "panel": "shared"
            },
            "problemMatcher": "\$msCompile",
            "dependsOrder": "sequence",
            "dependsOn": []
        }
    ]
}
EOF
    
    log_success "VS Code build integration configured"
}

# Create environment configuration file
create_env_file() {
    local vertical="$1"
    local repo_dir="$LOCATION_ROOT/Location-$vertical"
    
    if [[ ! -d "$repo_dir" ]]; then
        return 0
    fi
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would create environment configuration file"
        return 0
    fi
    
    # Get Minikube IP and SQL Server port
    local minikube_ip=""
    local sql_port=""
    
    if minikube status --profile=location-dev >/dev/null 2>&1; then
        minikube_ip=$(minikube ip --profile=location-dev 2>/dev/null || echo "localhost")
        sql_port=$(kubectl get service mssql-service -n location-dev -o jsonpath='{.spec.ports[0].nodePort}' 2>/dev/null || echo "1433")
    else
        minikube_ip="localhost"
        sql_port="1433"
    fi
    
    cat > "$repo_dir/.env" << EOF
# Location Development Environment Configuration
# Generated by setup-dev.sh on $(date)

# Database Configuration
DATABASE_HOST=$minikube_ip
DATABASE_PORT=$sql_port
DATABASE_NAME=DataConsumption
DATABASE_USER=sa
DATABASE_PASSWORD=LocationDev2024!
CONNECTION_STRING=Server=$minikube_ip,$sql_port;Database=DataConsumption;User Id=sa;Password=LocationDev2024!;TrustServerCertificate=true;

# Azure Configuration  
AZURE_ARTIFACTS_FEED=https://pkgs.dev.azure.com/x3squaredcircles/_packaging/LocationLibraries-Core/nuget/v3/index.json

# Development Settings
VERTICAL=$vertical
ENVIRONMENT=Development
LOG_LEVEL=Debug

# Kubernetes Configuration
MINIKUBE_PROFILE=location-dev
KUBERNETES_NAMESPACE=location-dev
EOF
    
    log_success "Environment configuration created: $repo_dir/.env"
}

# Uninstall function
uninstall_all() {
    log_info "Uninstalling Location development environment..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would uninstall all Location development components"
        return 0
    fi
    
    # Stop and delete Minikube cluster
    if command_exists minikube; then
        log_info "Stopping Minikube cluster..."
        minikube stop --profile=location-dev 2>/dev/null || true
        minikube delete --profile=location-dev 2>/dev/null || true
    fi
    
    # Uninstall Location tools
    local tools=(
        "location-version-calculator"
        "sql-schema-generator"
        "location-api-generator"
        "photography-adapter-generator"
        "location-pr-enhancer"
    )
    
    for tool in "${tools[@]}"; do
        if dotnet tool list -g | grep -q "$tool"; then
            log_info "Uninstalling $tool..."
            dotnet tool uninstall -g "$tool" 2>/dev/null || true
        fi
    done
    
    # Remove Location directory
    if [[ -d "$LOCATION_ROOT" ]]; then
        log_info "Removing Location directory..."
        rm -rf "$LOCATION_ROOT"
    fi
    
    log_success "Location development environment uninstalled"
}

# Parse command line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --photography|--fishing|--hunting)
                VERTICAL="${1#--}"
                ;;
            --tools)
                TOOLS_MODE="source"
                ;;
            --update)
                UPDATE_MODE=true
                ;;
            --uninstall)
                UNINSTALL_MODE=true
                ;;
            --debug)
                DEBUG_MODE=true
                set -x
                ;;
            --dry-run)
                DRY_RUN=true
                ;;
            --help|-h)
                show_help
                exit 0
                ;;
            *)
                fatal "Unknown option: $1"
                ;;
        esac
        shift
    done
    
    # Validation
    if [[ -z "$VERTICAL" && "$UNINSTALL_MODE" != true ]]; then
        fatal "Must specify a vertical: --photography, --fishing, or --hunting"
    fi
}

# Show help
show_help() {
    cat << EOF
Location Development Environment Setup

USAGE:
    ./setup-dev.sh --<vertical> [OPTIONS]

VERTICALS:
    --photography     Setup Photography vertical development
    --fishing         Setup Fishing vertical development  
    --hunting         Setup Hunting vertical development

OPTIONS:
    --tools           Build tools from source (for tool developers)
    --update          Update existing installation
    --uninstall       Remove all components
    --debug           Enable debug output
    --dry-run         Show what would be installed without executing
    --help, -h        Show this help message

EXAMPLES:
    # Standard developer setup
    ./setup-dev.sh --photography
    
    # Tool developer setup
    ./setup-dev.sh --photography --tools
    
    # Add another vertical to existing setup
    ./setup-dev.sh --fishing
    
    # Update all components
    ./setup-dev.sh --photography --update
    
    # Complete uninstall
    ./setup-dev.sh --uninstall

WHAT IT INSTALLS:
    - .NET 9 SDK
    - Git
    - Minikube + kubectl
    - Visual Studio Code + extensions
    - Azure CLI
    - SQL Server in Kubernetes
    - Location development tools
    - Repository clone and configuration

For more information, visit: https://dev.azure.com/x3squaredcircles/Location/_wiki
EOF
}

# Error handling
cleanup_on_error() {
    local line_number=$1
    log_error "Installation failed at line $line_number"
    log_error "Last command: $BASH_COMMAND"
    
    if [[ "$DEBUG_MODE" == false ]]; then
        log_error "Run with --debug for more detailed output"
    fi
    
    log_error "Installation incomplete. You may need to run with --uninstall to clean up."
    exit 1
}

cleanup_on_exit() {
    if [[ "$DEBUG_MODE" == true ]]; then
        set +x
    fi
}

# Main execution
main() {
    # Setup error handling
    trap 'cleanup_on_error $LINENO' ERR
    trap cleanup_on_exit EXIT
    
    # Parse arguments
    parse_arguments "$@"
    
    # Handle uninstall
    if [[ "$UNINSTALL_MODE" == true ]]; then
        uninstall_all
        exit 0
    fi
    
    # Show header
    echo ""
    log_info "Location Development Environment Setup"
    log_info "======================================="
    log_info "Platform: $PLATFORM"
    log_info "Vertical: $VERTICAL"
    log_info "Mode: $TOOLS_MODE"
    if [[ "$DRY_RUN" == true ]]; then
        log_warning "DRY RUN MODE - No changes will be made"
    fi
    echo ""
    
    # Detect platform
    detect_platform
    
    # Install core development tools
    log_info "Installing core development tools..."
    install_package_manager
    install_dotnet
    install_git
    install_minikube
    install_kubectl
    install_vscode
    install_vscode_extensions
    install_azure_cli
    
    # Authenticate with Azure
    authenticate_azure
    
    # Start Minikube and setup database
    log_info "Setting up Kubernetes development environment..."
    start_minikube
    setup_database
    
    # Clone and configure repository
    clone_repository "$VERTICAL"
    configure_git "$VERTICAL"
    
    # Install Location tools
    install_location_tools
    
    # Setup VS Code build integration
    setup_vscode_build_integration "$VERTICAL"
    
    # Create environment configuration
    create_env_file "$VERTICAL"
    
    log_success "Development environment setup completed successfully!"
    log_info ""
    log_info "Next steps:"
    log_info "1. Open VS Code: cd $LOCATION_ROOT/Location-$VERTICAL && code ."
    log_info "2. Database connection details: cat $LOCATION_ROOT/database-connection.txt"
    log_info "3. Build project: Press Ctrl+Shift+B (or Cmd+Shift+B on macOS) in VS Code"
    log_info "4. Minikube dashboard: minikube dashboard --profile=location-dev"
}

# Execute main function with all arguments
main "$@"
EOF
    
    # Create launch.json for debugging
    cat > "$vscode_dir/launch.json" << EOF
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "\${workspaceFolder}/bin/Debug/net9.0/Location.$vertical.dll",
            "args": [],
            "cwd": "\${workspaceFolder}",
            "console": "internalConsole",
            "stopAtEntry": false
        }
    ]#!/bin/bash
# setup-dev.sh - Location Development Environment Setup
# Single repository per vertical, Core dependencies via Azure Artifacts

set -euo pipefail

# Global variables
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOCATION_ROOT="$HOME/Location"
VERTICAL=""
TOOLS_MODE="artifacts"  # "artifacts" or "source"
UPDATE_MODE=false
UNINSTALL_MODE=false
DEBUG_MODE=false
DRY_RUN=false
PLATFORM=""
PACKAGE_MANAGER=""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" >&2
}

fatal() {
    log_error "$1"
    exit 1
}

# Platform detection
detect_platform() {
    case "$(uname -s)" in
        Darwin*)    PLATFORM="macos" ;;
        Linux*)     PLATFORM="linux" ;;
        MINGW*|MSYS*|CYGWIN*) PLATFORM="windows" ;;
        *) fatal "Unsupported platform: $(uname -s)" ;;
    esac
    
    log_info "Detected platform: $PLATFORM"
    
    # Detect package manager on Linux
    if [[ "$PLATFORM" == "linux" ]]; then
        if command -v apt-get >/dev/null; then
            PACKAGE_MANAGER="apt"
        elif command -v yum >/dev/null; then
            PACKAGE_MANAGER="yum"
        elif command -v pacman >/dev/null; then
            PACKAGE_MANAGER="pacman"
        else
            fatal "No supported package manager found (apt, yum, pacman)"
        fi
        log_info "Detected package manager: $PACKAGE_MANAGER"
    fi
}

# Command exists check
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Version comparison
version_gt() {
    test "$(printf '%s\n' "$@" | sort -V | head -n 1)" != "$1"
}

# Azure authentication
authenticate_azure() {
    log_info "Authenticating with Azure AD..."
    
    if ! az account show >/dev/null 2>&1; then
        log_info "Please sign in to Azure AD"
        az login --scope https://pkgs.dev.azure.com/.default
    fi
    
    local tenant_id=$(az account show --query tenantId -o tsv)
    local user_principal=$(az account show --query user.name -o tsv)
    
    log_info "Authenticated as: $user_principal"
    
    # Test Azure Artifacts access
    if ! az artifacts universal list \
        --organization https://dev.azure.com/x3squaredcircles \
        --feed x3squaredcircles-tools >/dev/null 2>&1; then
        fatal "No access to Azure Artifacts. Contact IT for permissions."
    fi
    
    log_success "Azure authentication successful"
}

# Install platform package manager
install_package_manager() {
    case $PLATFORM in
        "macos")
            if ! command_exists brew; then
                log_info "Installing Homebrew..."
                if [[ "$DRY_RUN" == false ]]; then
                    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
                fi
            else
                log_info "Homebrew already installed"
            fi
            ;;
        "windows")
            if ! command_exists choco; then
                log_info "Installing Chocolatey..."
                if [[ "$DRY_RUN" == false ]]; then
                    powershell -Command "Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))"
                fi
            else
                log_info "Chocolatey already installed"
            fi
            ;;
        "linux")
            # Package managers are built-in on Linux
            log_info "Using system package manager: $PACKAGE_MANAGER"
            ;;
    esac
}

# Install .NET 9 SDK
install_dotnet() {
    local required_version="9.0"
    local installed_version=""
    
    if command_exists dotnet; then
        installed_version=$(dotnet --version 2>/dev/null | cut -d. -f1-2 || echo "none")
    else
        installed_version="none"
    fi
    
    if [[ "$installed_version" == "$required_version"* ]]; then
        log_info ".NET $installed_version already installed"
        return 0
    fi
    
    log_info "Installing .NET $required_version SDK..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install .NET $required_version SDK"
        return 0
    fi
    
    case $PLATFORM in
        "macos")
            brew install --cask dotnet-sdk
            ;;
        "linux")
            case $PACKAGE_MANAGER in
                "apt")
                    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
                    sudo dpkg -i packages-microsoft-prod.deb
                    rm packages-microsoft-prod.deb
                    sudo apt-get update
                    sudo apt-get install -y dotnet-sdk-9.0
                    ;;
                "yum")
                    sudo rpm -Uvh https://packages.microsoft.com/config/fedora/37/packages-microsoft-prod.rpm
                    sudo yum install -y dotnet-sdk-9.0
                    ;;
            esac
            ;;
        "windows")
            choco install dotnet-9.0-sdk -y
            ;;
    esac
    
    # Verify installation
    if command_exists dotnet; then
        local new_version=$(dotnet --version)
        log_success ".NET SDK $new_version installed successfully"
    else
        fatal "Failed to install .NET SDK"
    fi
}

# Install Git
install_git() {
    if command_exists git; then
        log_info "Git already installed: $(git --version)"
        return 0
    fi
    
    log_info "Installing Git..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install Git"
        return 0
    fi
    
    case $PLATFORM in
        "macos")
            brew install git
            ;;
        "linux")
            case $PACKAGE_MANAGER in
                "apt")
                    sudo apt-get update
                    sudo apt-get install -y git
                    ;;
                "yum")
                    sudo yum install -y git
                    ;;
                "pacman")
                    sudo pacman -S --noconfirm git
                    ;;
            esac
            ;;
        "windows")
            choco install git -y
            ;;
    esac
    
    log_success "Git installed successfully"
}

# Install Minikube
install_minikube() {
    if command_exists minikube; then
        log_info "Minikube already installed: $(minikube version --short)"
        return 0
    fi
    
    log_info "Installing Minikube..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install Minikube"
        return 0
    fi
    
    case $PLATFORM in
        "macos")
            brew install minikube
            ;;
        "linux")
            curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
            sudo install minikube-linux-amd64 /usr/local/bin/minikube
            rm minikube-linux-amd64
            ;;
        "windows")
            choco install minikube -y
            ;;
    esac
    
    log_success "Minikube installed successfully"
}

# Install kubectl
install_kubectl() {
    if command_exists kubectl; then
        log_info "kubectl already installed: $(kubectl version --client --short 2>/dev/null || kubectl version --client)"
        return 0
    fi
    
    log_info "Installing kubectl..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install kubectl"
        return 0
    fi
    
    case $PLATFORM in
        "macos")
            brew install kubectl
            ;;
        "linux")
            case $PACKAGE_MANAGER in
                "apt")
                    sudo apt-get update
                    sudo apt-get install -y apt-transport-https ca-certificates curl
                    curl -fsSL https://pkgs.k8s.io/core:/stable:/v1.28/deb/Release.key | sudo gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg
                    echo 'deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/v1.28/deb/ /' | sudo tee /etc/apt/sources.list.d/kubernetes.list
                    sudo apt-get update
                    sudo apt-get install -y kubectl
                    ;;
                "yum")
                    cat <<EOF | sudo tee /etc/yum.repos.d/kubernetes.repo
[kubernetes]
name=Kubernetes
baseurl=https://pkgs.k8s.io/core:/stable:/v1.28/rpm/
enabled=1
gpgcheck=1
gpgkey=https://pkgs.k8s.io/core:/stable:/v1.28/rpm/repodata/repomd.xml.key
EOF
                    sudo yum install -y kubectl
                    ;;
            esac
            ;;
        "windows")
            choco install kubernetes-cli -y
            ;;
    esac
    
    log_success "kubectl installed successfully"
}

# Install Visual Studio Code
install_vscode() {
    if command_exists code; then
        log_info "VS Code already installed: $(code --version | head -n1)"
        return 0
    fi
    
    log_info "Installing Visual Studio Code..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install Visual Studio Code"
        return 0
    fi
    
    case $PLATFORM in
        "macos")
            brew install --cask visual-studio-code
            ;;
        "linux")
            case $PACKAGE_MANAGER in
                "apt")
                    wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > packages.microsoft.gpg
                    sudo install -o root -g root -m 644 packages.microsoft.gpg /etc/apt/trusted.gpg.d/
                    sudo sh -c 'echo "deb [arch=amd64,arm64,armhf signed-by=/etc/apt/trusted.gpg.d/packages.microsoft.gpg] https://packages.microsoft.com/repos/code stable main" > /etc/apt/sources.list.d/vscode.list'
                    sudo apt-get update
                    sudo apt-get install -y code
                    ;;
            esac
            ;;
        "windows")
            choco install vscode -y
            ;;
    esac
    
    log_success "VS Code installed successfully"
}

# Install VS Code extensions
install_vscode_extensions() {
    if ! command_exists code; then
        log_warning "VS Code not found, skipping extension installation"
        return 0
    fi
    
    log_info "Installing VS Code extensions..."
    
    local extensions=(
        "ms-dotnettools.csharp"
        "ms-dotnettools.vscode-dotnet-runtime"
        "ms-vscode.azure-account"
        "eamodio.gitlens"
        "rangav.vscode-thunder-client"
        "ms-azuretools.vscode-docker"
    )
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install VS Code extensions: ${extensions[*]}"
        return 0
    fi
    
    for extension in "${extensions[@]}"; do
        log_info "Installing extension: $extension"
        code --install-extension "$extension" --force
    done
    
    log_success "VS Code extensions installed successfully"
}

# Install Azure CLI
install_azure_cli() {
    if command_exists az; then
        log_info "Azure CLI already installed: $(az --version | head -n1 | cut -d' ' -f2)"
        return 0
    fi
    
    log_info "Installing Azure CLI..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would install Azure CLI"
        return 0
    fi
    
    case $PLATFORM in
        "macos")
            brew install azure-cli
            ;;
        "linux")
            case $PACKAGE_MANAGER in
                "apt")
                    curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
                    ;;
                "yum")
                    sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
                    sudo sh -c 'echo -e "[azure-cli]\nname=Azure CLI\nbaseurl=https://packages.microsoft.com/yumrepos/azure-cli\nenabled=1\ngpgcheck=1\ngpgkey=https://packages.microsoft.com/keys/microsoft.asc" > /etc/yum.repos.d/azure-cli.repo'
                    sudo yum install -y azure-cli
                    ;;
            esac
            ;;
        "windows")
            choco install azure-cli -y
            ;;
    esac
    
    log_success "Azure CLI installed successfully"
}

# Start Minikube cluster
start_minikube() {
    log_info "Starting Minikube cluster..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would start Minikube cluster"
        return 0
    fi
    
    # Check if Minikube is already running
    if minikube status >/dev/null 2>&1; then
        log_info "Minikube cluster already running"
        return 0
    fi
    
    # Start Minikube with recommended settings for development
    log_info "Starting Minikube cluster (this may take a few minutes)..."
    minikube start \
        --cpus=4 \
        --memory=8192 \
        --disk-size=20gb \
        --kubernetes-version=v1.28.0 \
        --driver=docker \
        --profile=location-dev
    
    # Enable necessary addons
    log_info "Enabling Minikube addons..."
    minikube addons enable ingress --profile=location-dev
    minikube addons enable dashboard --profile=location-dev
    minikube addons enable metrics-server --profile=location-dev
    
    # Set kubectl context
    kubectl config use-context location-dev
    
    log_success "Minikube cluster started successfully"
}

# Setup development database in Kubernetes
setup_database() {
    log_info "Setting up development database in Kubernetes..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would setup SQL Server in Kubernetes and DataConsumption database"
        return 0
    fi
    
    # Ensure Minikube is running
    if ! minikube status --profile=location-dev >/dev/null 2>&1; then
        log_info "Minikube not running, starting cluster..."
        start_minikube
    fi
    
    # Create namespace for Location development
    kubectl create namespace location-dev --dry-run=client -o yaml | kubectl apply -f -
    
    # Create SQL Server deployment and service
    log_info "Creating SQL Server deployment..."
    cat <<EOF | kubectl apply -f -
apiVersion: v1
kind: Secret
metadata:
  name: mssql-secret
  namespace: location-dev
type: Opaque
data:
  SA_PASSWORD: TG9jYXRpb25EZXY yMDI0IQ==  # LocationDev2024! base64 encoded
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: mssql-data-pvc
  namespace: location-dev
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mssql-deployment
  namespace: location-dev
spec:
  replicas: 1
  selector:
    matchLabels:
      app: mssql
  template:
    metadata:
      labels:
        app: mssql
    spec:
      containers:
      - name: mssql
        image: mcr.microsoft.com/mssql/server:2022-CU8-ubuntu-20.04
        env:
        - name: ACCEPT_EULA
          value: "Y"
        - name: MSSQL_PID
          value: "Developer"
        - name: SA_PASSWORD
          valueFrom:
            secretKeyRef:
              name: mssql-secret
              key: SA_PASSWORD
        ports:
        - containerPort: 1433
        volumeMounts:
        - name: mssql-data
          mountPath: /var/opt/mssql/data
        resources:
          requests:
            memory: "2Gi"
            cpu: "500m"
          limits:
            memory: "4Gi"
            cpu: "2000m"
      volumes:
      - name: mssql-data
        persistentVolumeClaim:
          claimName: mssql-data-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: mssql-service
  namespace: location-dev
spec:
  selector:
    app: mssql
  ports:
  - protocol: TCP
    port: 1433
    targetPort: 1433
  type: NodePort
EOF
    
    # Wait for SQL Server pod to be ready
    log_info "Waiting for SQL Server to be ready..."
    kubectl wait --for=condition=ready pod -l app=mssql -n location-dev --timeout=300s
    
    # Get the NodePort for SQL Server
    local nodeport=$(kubectl get service mssql-service -n location-dev -o jsonpath='{.spec.ports[0].nodePort}')
    local minikube_ip=$(minikube ip --profile=location-dev)
    
    log_info "SQL Server accessible at: $minikube_ip:$nodeport"
    
    # Wait a bit more for SQL Server to fully start
    sleep 30
    
    # Create DataConsumption database
    log_info "Creating DataConsumption database..."
    local pod_name=$(kubectl get pods -n location-dev -l app=mssql -o jsonpath='{.items[0].metadata.name}')
    
    kubectl exec -n location-dev "$pod_name" -- /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa -P "LocationDev2024!" \
        -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'DataConsumption') CREATE DATABASE DataConsumption"
    
    # Create connection string file for easy reference
    mkdir -p "$LOCATION_ROOT"
    cat > "$LOCATION_ROOT/database-connection.txt" << EOF
SQL Server Connection Details:
Host: $minikube_ip
Port: $nodeport
Database: DataConsumption
Username: sa
Password: LocationDev2024!

Connection String:
Server=$minikube_ip,$nodeport;Database=DataConsumption;User Id=sa;Password=LocationDev2024!;TrustServerCertificate=true;
EOF
    
    log_success "Development database ready in Kubernetes"
    log_info "Connection details saved to: $LOCATION_ROOT/database-connection.txt"
}

# Clone repository
clone_repository() {
    local vertical="$1"
    local repo_dir="$LOCATION_ROOT/Location-$vertical"
    
    if [[ -d "$repo_dir" ]]; then
        log_info "Repository Location-$vertical already exists"
        if [[ "$UPDATE_MODE" == true ]]; then
            log_info "Updating repository..."
            cd "$repo_dir"
            git pull
        fi
        return 0
    fi
    
    log_info "Cloning Location-$vertical repository..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would clone Location-$vertical repository"
        return 0
    fi
    
    mkdir -p "$LOCATION_ROOT"
    cd "$LOCATION_ROOT"
    
    git clone "https://dev.azure.com/x3squaredcircles/Location/_git/Location-$vertical"
    
    log_success "Repository cloned successfully"
}

# Configure Git for repository
configure_git() {
    local vertical="$1"
    local repo_dir="$LOCATION_ROOT/Location-$vertical"
    
    if [[ ! -d "$repo_dir" ]]; then
        log_warning "Repository directory not found, skipping Git configuration"
        return 0
    fi
    
    cd "$repo_dir"
    
    log_info "Configuring Git repository..."
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would configure Git repository"
        return 0
    fi
    
    # Setup .gitignore if it doesn't exist
    if [[ ! -f ".gitignore" ]]; then
        cat > .gitignore << 'EOF'
# Build Artifacts
**/bin/
**/obj/
*.dll
*.pdb
*.exe
!*.exe.config

# Generated Mobile Adapters (NEVER commit)
**/generated/
**/AndroidUI/generated/
**/iOSUI/generated/
**/*Adapter.kt
**/*Adapter.swift

# Tool Outputs
**/tools/bin/
**/tools/obj/

# Azure Artifacts Cache
**/.nuget/
**/packages/

# IDE
.vs/
.vscode/settings.json
*.user
*.suo

# OS
.DS_Store
Thumbs.db
EOF
        git add .gitignore
        git commit -m "Add standard .gitignore for Location project"
    fi
    
    # Setup nuget.config for Azure Artifacts
    if [[ ! -f "nuget.config" ]]; then
        cat > nuget.config << 'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="Azure Artifacts - Core" value="https://pkgs.dev.azure.com/x3squaredcircles/_packaging/LocationLibraries-Core/nuget/v3/index.json" />
    <add key="Azure Artifacts - Tools" value="https://pkgs.dev.azure.com/x3squaredcircles/_packaging/x3squaredcircles-tools/nuget/v3/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <Azure_Artifacts_-_Core>
      <add key="Username" value="AzureDevOps" />
      <add key="ClearTextPassword" value="%AZURE_ARTIFACTS_TOKEN%" />
    </Azure_Artifacts_-_Core>
    <Azure_Artifacts_-_Tools>
      <add key="Username" value="AzureDevOps" />
      <add key="ClearTextPassword" value="%AZURE_ARTIFACTS_TOKEN%" />
    </Azure_Artifacts_-_Tools>
  </packageSourceCredentials>
</configuration>
EOF
        git add nuget.config
        git commit -m "Add Azure Artifacts package configuration"
    fi
    
    log_success "Git repository configured"
}

# Install Location tools
install_location