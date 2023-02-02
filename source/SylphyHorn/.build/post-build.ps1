# ビルド後に実行
# - SylphyHorn.exe (とその関連ファイル) を除くすべてのファイルを lib フォルダーに移動


Param ( $TargetDir )

Write-Output "$TargetDir"
Write-Output $TargetDir 

$targets = $TargetDir
$lib = ($TargetDir + "lib\")
$excludes = ".assets", "AppxManifest.xml", "SylphyHorn.exe*", "SylphyHorn.pdb"


Write-Output "$targets"
Write-Output $targets 
Write-Output "$lib"
Write-Output $lib 
Write-Output "$excludes"
Write-Output $excludes 


if ( Test-Path $lib ) {
    Remove-Item $lib -Recurse
}

New-Item $lib -ItemType Directory


Get-Help Test-Path -Full -Path $targets -Exclude $excludes

Get-ChildItem $targets -Exclude $excludes | Move-Item -Destination $lib


Function Wait-FileUnlock{
    Param(
        [Parameter()]
        [IO.FileInfo]$File,
        [int]$SleepInterval=500
    )
    while(1){
        try{
           $fs=$file.Open('open','read', 'Read')
           $fs.Close()
            Write-Verbose "$file not open"
           return
           }
        catch{
           Start-Sleep -Milliseconds $SleepInterval
           Write-Verbose '-'
        }
	}
}