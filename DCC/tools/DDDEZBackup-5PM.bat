
for /f "tokens=1-4 delims=/ " %%i in ("%date%") do (
     set dow=%%i
     set month=%%j
     set day=%%k
     set year=%%l
)
ECHO %day%
set Bkup=F:\SQLBACPAC\DDDEZ\DDDEZ%day%-5PM.bacpac
ECHO %Bkup%
"C:\Program Files (x86)\Microsoft SQL Server\140\DAC\bin\sqlpackage.exe" /a:Export /ssn:.\SQLEXPRESS2016 /sdn:DDDEZ /tf:%Bkup%

