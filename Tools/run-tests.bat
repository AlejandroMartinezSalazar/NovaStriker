@echo off
rem ============================================================================
rem  NovaStriker - ejecuta todas las pruebas EditMode sin abrir el Editor.
rem
rem  Uso:   Tools\run-tests.bat
rem  Antes: CIERRA Unity (no se puede abrir el mismo proyecto dos veces).
rem
rem  Busca Unity 6000.6.5f1 en la ruta por defecto de Unity Hub. Si lo tienes en
rem  otra carpeta, define antes la variable UNITY_PATH, por ejemplo:
rem     set UNITY_PATH=D:\Unity\6000.6.5f1\Editor\Unity.exe
rem
rem  Resultado: TestResults\editmode-results.xml (formato NUnit) y
rem             TestResults\editmode.log (registro completo de Unity).
rem  Código de salida: 0 = todo en verde, 2 = alguna prueba falló, otro = error.
rem ============================================================================
setlocal

set "PROJECT=%~dp0.."
for %%I in ("%PROJECT%") do set "PROJECT=%%~fI"

if "%UNITY_PATH%"=="" set "UNITY_PATH=C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe"
if not exist "%UNITY_PATH%" (
    echo [ERROR] No encuentro Unity en "%UNITY_PATH%".
    echo         Define UNITY_PATH con la ruta de Unity.exe 6000.6.5f1 y vuelve a ejecutar.
    exit /b 1
)

if not exist "%PROJECT%\TestResults" mkdir "%PROJECT%\TestResults"
set "RESULTS=%PROJECT%\TestResults\editmode-results.xml"
set "LOG=%PROJECT%\TestResults\editmode.log"

echo Proyecto : %PROJECT%
echo Unity    : %UNITY_PATH%
echo Ejecutando pruebas EditMode (la primera vez tarda varios minutos: Unity importa el proyecto)...

"%UNITY_PATH%" -batchmode -nographics -projectPath "%PROJECT%" -runTests -testPlatform EditMode -testResults "%RESULTS%" -logFile "%LOG%"
set "CODE=%ERRORLEVEL%"

if exist "%RESULTS%" (
    powershell -NoProfile -Command "$r = [xml](Get-Content -LiteralPath '%RESULTS%'); $t = $r.'test-run'; Write-Host ''; Write-Host ('Total: {0}   Correctas: {1}   Fallidas: {2}   Omitidas: {3}   Duracion: {4:N1} s' -f $t.total, $t.passed, $t.failed, $t.skipped, [double]$t.duration); if ([int]$t.failed -gt 0) { Write-Host ''; Write-Host 'Pruebas fallidas:'; $r.SelectNodes('//test-case[@result=''Failed'']') | ForEach-Object { Write-Host (' - ' + $_.fullname) } }"
) else (
    echo [ERROR] Unity no generó resultados. Revisa %LOG%
)

echo.
if "%CODE%"=="0" (echo RESULTADO: todas las pruebas pasaron.) else (echo RESULTADO: codigo %CODE% ^(2 = hay pruebas fallidas; otro = error, ver %LOG%^).)
exit /b %CODE%
