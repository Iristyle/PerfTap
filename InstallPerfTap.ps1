param
(
	[Parameter()]
	[Hashtable] $settings = @{}
)

Set-StrictMode -version Latest

$latestBuild = 'https://github.com/downloads/Iristyle/PerfTap/PerfTap-0.1.1.zip'

function Test-IsAdmin   
{  
	$identity = [Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
    If (-NOT $identity.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
    {  
		throw 'You are not currently running this installation under an Administrator account.  Installation aborted!'
	}
}

function Merge-Parameters()
{
	param
	(
		[Parameter(Mandatory=$true)]
		[Hashtable] $Hash
	)
	
	$defaults = 
	@{
		Key = '';
		Port = 8125;
		SampleInterval = '00:00:05';
		DefinitionPaths = @('CounterDefinitions\system.counters');
		CounterNames = @()
	}
	
	$allowedKeys = ($defaults | Select -ExpandProperty Keys) + @('HostName','Format')
	$Hash | Select -ExpandProperty Keys | 
	% {
		if (-not $allowedKeys -contains $_)
		{
			$msg = "Parameter $_ not expected"
			Write-Error -Message $msg -Category InvalidArgument
			throw $msg
		}
		$defaults.$_ = $Hash.$_ 
	}
	
	$defaults
}

function Extract-Zip()
{
    Write-Host "Downloading Latest Package from $latestBuild"
	$path = "${Env:Temp}\perftap.zip"
    (New-Object Net.WebClient).DownloadFile($latestBuild, $path)
	$shellApplication = New-Object -com Shell.Application
	$zipItems = $shellApplication.NameSpace($path).Items()
	$extracted = "${Env:ProgramFiles}\PerfTap"
	if (!(test-path $extracted)) { [Void](New-Item $extracted -type directory) }
	$shellApplication.NameSpace($extracted).CopyHere($zipItems, 0x14)
	Remove-Item $path
}

function Run-ServiceInstaller()
{
	Set-Alias installutil $Env:windir\Microsoft.NET\Framework\v4.0.30319\installutil.exe
	$servicePath = "${Env:ProgramFiles}\PerfTap\PerfTap.WindowsServiceHost.exe"
	if ((Get-Service PerfTap -ErrorAction SilentlyContinue) -ne $null)
	{
		installutil /u """$servicePath"""
	}
	
	installutil """$servicePath"""
}

function Modify-ConfigFile()
{
	param
	(
	    [Parameter(Mandatory=$true)]
		[AllowEmptyCollection()]
	    [string[]]
	    $CounterNames,
	    
	    [parameter(Mandatory=$true)]
	    [string[]]
	    $DefinitionPaths,

	    [parameter(Mandatory=$true)]
	    [string]
	    $HostName,
	    
	    [parameter(Mandatory=$false)]
	    [int]
	    $Port = 8125,

	    [parameter(Mandatory=$false)]
	    [TimeSpan]
	    [ValidateRange('00:00:01', '00:05:00')] # 1s -> 5m
	    $SampleInterval = [TimeSpan]::FromSeconds(5),
	    
	    [parameter(Mandatory=$false)]
	    [string]
	    [ValidatePattern('^[^!\s;:/\.\(\)\\#%\$\^]+$|^$')]
	    $Key = '',

		[ValidateSet("StatsD","StatSite")] 
		[string]
		$Format
	)

	$path = "${Env:ProgramFiles}\PerfTap\PerfTap.WindowsServiceHost.exe.config"
	$xml = New-Object Xml
	$xml.Load($path)
	
	$xml.configuration.perfTapCounterSampling.SetAttribute('sampleInterval', $SampleInterval)
	
	$definitionFilePathsNode = $xml.SelectSingleNode('//definitionFilePaths')
	if ($xml.SelectSingleNode('//definitionFilePaths') -ne $null)
	{
		$definitionFilePathsNode.RemoveAll()
	}
	else
	{
		$definitionFilePathsNode = $xml.CreateElement('definitionFilePaths')
		[Void]$xml.configuration.perfTapCounterSampling.AppendChild($definitionFilePathsNode)
	}
	
	$DefinitionPaths | % {
			$filePath = $xml.CreateElement('definitionFile')
			$filePath.SetAttribute('path', $_)
			[Void]$definitionFilePathsNode.AppendChild($filePath)
		}
	
	$counterNamesNode = $xml.SelectSingleNode('//counterNames')
	if ($counterNamesNode -ne $null)
	{
		$counterNamesNode.RemoveAll()
	}
	else
	{
		$counterNamesNode = $xml.CreateElement('counterNames')
		[Void]$xml.configuration.perfTapCounterSampling.AppendChild($counterNamesNode)
	}
	
	$CounterNames | % {
			$name = $xml.CreateElement('counter')
			$name.SetAttribute('name', $_)
			[Void]$counterNamesNode.AppendChild($name)
		}
	
	$xml.configuration.perfTapPublishing.SetAttribute("prefixKey",$Key)
	$xml.configuration.perfTapPublishing.SetAttribute("port",$Port)
	$xml.configuration.perfTapPublishing.SetAttribute("hostName",$HostName)	
	$xml.configuration.perfTapPublishing.SetAttribute("format",$Format)	
	
	$xml.Save($path)
}

function Install-Service()
{    
	[CmdletBinding()]
	param
	(   		     
	    [parameter(Mandatory=$true)]
		[AllowEmptyCollection()]
	    [string[]]
	    $CounterNames,
	    
	    [parameter(Mandatory=$true)]
	    [string[]]
	    $DefinitionPaths,

	    [parameter(Mandatory=$true)]
	    [string]
	    $HostName,
	    
	    [parameter(Mandatory=$false)]
	    [int]
	    $Port = 8125,

	    [parameter(Mandatory=$false)]
	    [TimeSpan]
	    [ValidateRange("00:00:01", "00:05:00")] # 1s -> 5m
	    $SampleInterval = [TimeSpan]::FromSeconds(5),
	    
	    [parameter(Mandatory=$false)]
	    [string]
	    [ValidatePattern('^[^!\s;:/\.\(\)\\#%\$\^]+$|^$')]
	    $Key = '',

		[ValidateSet("StatsD","StatSite")] 
		[string]
		$Format
	)
	
    $executionPolicy  = (Get-ExecutionPolicy)
    $executionRestricted = ($executionPolicy -eq "Restricted")
    if ($executionRestricted){
        Write-Warning @"
Your execution policy is $executionPolicy, this means you will not be able import or use any scripts including modules.
To fix this change you execution policy to something like RemoteSigned.

        PS> Set-ExecutionPolicy RemoteSigned

For more information execute:
        
        PS> Get-Help about_execution_policies

"@
    }
	else
	{
		if ((Get-Service PerfTap -ErrorAction SilentlyContinue) -ne $null)
		{
			Stop-Service PerfTap
		}
		Extract-Zip
		Run-ServiceInstaller
		Modify-ConfigFile -Key $Key -Port $Port -SampleInterval $SampleInterval -HostName $HostName `
			-CounterNames $CounterNames -DefinitionPaths $DefinitionPaths
		Start-Service PerfTap	
	}
}

Test-IsAdmin
$mergedSettings = Merge-Parameters -Hash $settings
Install-Service @mergedSettings
