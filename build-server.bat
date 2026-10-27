@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0server"

where go >nul 2>nul
if errorlevel 1 (
    echo [错误] 没找到 go 命令，请先安装 Go 1.22 或更高版本。
    echo.
    pause
    exit /b 1
)

echo 正在编译 inspection-server.exe ...
go build -o inspection-server.exe .
if errorlevel 1 (
    echo.
    echo [错误] 编译失败，看上面的报错信息。
    echo.
    pause
    exit /b 1
)

echo.
echo 编译完成: %~dp0server\inspection-server.exe
echo 之后直接双击 start-backend.bat 即可启动。
echo.
pause
