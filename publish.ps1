# HoverPortal 发布脚本
# 使用方法: .\publish.ps1 [-Mode fdd|scd|all]

param(
    [ValidateSet("fdd", "scd", "all")]
    [string]$Mode = "all"
)

$ErrorActionPreference = "Stop"
$ProjectPath = "$PSScriptRoot\src\HoverPortal\HoverPortal.csproj"
$OutputBase = "$PSScriptRoot\publish"

# 清理输出目录
function Clear-PublishFolder {
    param([string]$Path)
    if (Test-Path $Path) {
        Remove-Item $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

# 发布框架依赖版本 (Framework-Dependent Deployment)
function Publish-FDD {
    Write-Host "`n=== 发布框架依赖版本 (FDD) ===" -ForegroundColor Cyan
    Write-Host "特点: 体积最小，需要用户安装 .NET 7 Runtime`n" -ForegroundColor Gray
    
    $output = "$OutputBase\fdd"
    Clear-PublishFolder $output
    
    dotnet publish $ProjectPath `
        -c Release `
        -r win-x64 `
        --self-contained false `
        -o $output
    
    if ($LASTEXITCODE -eq 0) {
        $exe = Get-ChildItem "$output\*.exe" | Select-Object -First 1
        $sizeMB = [math]::Round($exe.Length / 1MB, 2)
        Write-Host "`n✅ FDD 发布成功!" -ForegroundColor Green
        Write-Host "   文件: $($exe.Name)" -ForegroundColor White
        Write-Host "   大小: $sizeMB MB" -ForegroundColor Yellow
        Write-Host "   路径: $output" -ForegroundColor Gray
    } else {
        Write-Host "❌ FDD 发布失败!" -ForegroundColor Red
    }
}

# 发布自包含版本 (Self-Contained Deployment)
function Publish-SCD {
    Write-Host "`n=== 发布自包含版本 (SCD + Trimming) ===" -ForegroundColor Cyan
    Write-Host "特点: 独立运行，无需安装 Runtime`n" -ForegroundColor Gray
    
    $output = "$OutputBase\scd"
    Clear-PublishFolder $output
    
    dotnet publish $ProjectPath `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishTrimmed=true `
        -o $output
    
    if ($LASTEXITCODE -eq 0) {
        $exe = Get-ChildItem "$output\*.exe" | Select-Object -First 1
        $sizeMB = [math]::Round($exe.Length / 1MB, 2)
        Write-Host "`n✅ SCD 发布成功!" -ForegroundColor Green
        Write-Host "   文件: $($exe.Name)" -ForegroundColor White
        Write-Host "   大小: $sizeMB MB" -ForegroundColor Yellow
        Write-Host "   路径: $output" -ForegroundColor Gray
    } else {
        Write-Host "❌ SCD 发布失败!" -ForegroundColor Red
    }
}

# 主逻辑
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "   HoverPortal 轻量化打包脚本" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

switch ($Mode) {
    "fdd" { Publish-FDD }
    "scd" { Publish-SCD }
    "all" {
        Publish-FDD
        Publish-SCD
        
        Write-Host "`n========================================" -ForegroundColor Magenta
        Write-Host "   发布完成 - 大小对比" -ForegroundColor Magenta
        Write-Host "========================================" -ForegroundColor Magenta
        
        $fddExe = Get-ChildItem "$OutputBase\fdd\*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
        $scdExe = Get-ChildItem "$OutputBase\scd\*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
        
        if ($fddExe) {
            $fddSize = [math]::Round($fddExe.Length / 1MB, 2)
            Write-Host "FDD (框架依赖): $fddSize MB" -ForegroundColor Cyan
        }
        if ($scdExe) {
            $scdSize = [math]::Round($scdExe.Length / 1MB, 2)
            Write-Host "SCD (自包含):   $scdSize MB" -ForegroundColor Cyan
        }
        
        Write-Host ""
    }
}
