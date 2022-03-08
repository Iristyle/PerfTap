param(
	[string]
	[Parameter(Position=0,ValueFromPipeLine=$true)]
	$MachineIP,
	[string]
	[Parameter(Position=1,ValueFromPipeLine=$true)]
	$UserName,
	[string]
	[Parameter(Position=2,ValueFromPipeLine=$true)]
	$Password,
	[string]
	[Parameter(Position=3,ValueFromPipeLine=$true)]
	$Key,
	[string]
	[Parameter(Position=4,ValueFromPipeLine=$true)]
	$Server,
	[int]
	[Parameter(Position=5,ValueFromPipeLine=$true)]
	$SecondFrequency,
	[string]
	[Parameter(Position=6,ValueFromPipeLine=$true)]
	$FilePath,
	[Array]
	[Parameter(Position=7,ValueFromPipeLine=$true)]
	$Counters
	)
	
Function GetFiles 
{
	param(
		[string]
		[Parameter(Mandatory=$true)]
		$filePath
	)
	
	$files=@{}
	If (Test-Path $filePath)
	{
			Get-ChildItem $filePath | % {$files.Add($_.Name , (Get-Content $_.FullName)) } 
			return $files
	}
	else 
	{
		Write-Host "File does not exist"
	}
}

GetFiles $FilePath

$params = @{ 
			FilePath = Join-Path (Split-Path ($MyInvocation.Command.Path) "remoteinstall.ps1");
			ArgumentList = @($Key,$Server,$SecondFrequency,$Counters,(GetFiles $FilePath),$UserName,$Password);
			ComputerName = $MachineIP;
			Credential = (New-Object System.Management.Automation.PsCredential($UserName,(ConvertTo-SecureString $Password -AsPlainText -force)))
		}
		
		Invoke-Command @params





	
	