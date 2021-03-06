$public = @( Get-ChildItem -Path $PSScriptRoot\public\*.ps1 -ErrorAction SilentlyContinue )
$private = @( Get-ChildItem -Path $PSScriptRoot\private\*.ps1 -ErrorAction SilentlyContinue )
$classes = @( Get-ChildItem -Path $PSScriptRoot\classes\*.ps1 -ErrorAction SilentlyContinue )

#Dot source the files
Foreach ($import in @($public + $private + $classes))
{
    Write-Verbose $import 

    Try
    {
        . $import.fullname
    }
    Catch
    {
        Throw "Failed to import function $($import.fullname): $_"
    }
}

##Only Export Public Functions
Export-ModuleMember -Function $Public.Basename
Set-StrictMode -Version 2.0