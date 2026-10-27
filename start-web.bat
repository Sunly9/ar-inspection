@echo off
chcp 65001 >nul
setlocal
title 巡检管理端 (Vite) - http://127.0.0.1:5173
cd /d "%~dp0web"

where npm >nul 2>nul
if errorlevel 1 (
    echo [错误] 没找到 npm，请先安装 Node.js（建议 18 以上）。
    echo.
    pause
    exit /b 1
)

if not exist "node_modules" (
    echo 首次运行，正在安装前端依赖，可能要一两分钟 ...
    call npm install
    if errorlevel 1 (
        echo [错误] 依赖安装失败，检查网络后重试。
        echo.
        pause
        exit /b 1
    )
)

echo.
echo 地址: http://127.0.0.1:5173 （浏览器会在几秒后自动打开）
echo 关闭本窗口或按 Ctrl+C 即可停止。
echo.
start "" cmd /c "timeout /t 4 /nobreak >nul && start http://127.0.0.1:5173"

call npm run dev

echo.
echo 前端已退出。
pause
