function Get-KarenTestPort([int]$DefaultPort) {
    if ($env:KAREN_TEST_MCP_PORT_BASE -and $DefaultPort -ge 15627 -and $DefaultPort -le 15630) {
        return [int]$env:KAREN_TEST_MCP_PORT_BASE + $DefaultPort - 15627
    }
    return $DefaultPort
}
function Get-KarenTestNetworkPort {
    if ($env:KAREN_TEST_NETWORK_PORT) { return [int]$env:KAREN_TEST_NETWORK_PORT }
    return 33771
}

function Get-KarenTestRenderArguments {
    if ($env:KAREN_TEST_RENDER_METHOD -eq 'gl_compatibility') { return '--rendering-method gl_compatibility' }
    return '--rendering-driver vulkan'
}
function Get-KarenTestPython {
    if ($env:KAREN_TEST_PYTHON) { return $env:KAREN_TEST_PYTHON }
    return (Get-Command python -CommandType Application -ErrorAction Stop).Source
}
