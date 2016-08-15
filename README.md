# System.Configuration for .NET Core

Compatible with NET Standard >= 1.5

- Bring back (partially) the good old [System.Configuration.ConfigurationManager](https://msdn.microsoft.com/en-us/library/system.configuration.configurationmanager(v=vs.110).aspx)
- Encryption is not supported
- Machine & Roaming configurations are not supported
- Requires manual initialization of the ConfigurationManager to specify the path to the *.config file
- Not fully tested: "Required" attribute doesn't work, probably other stuff too
- Code adapted from Mono (https://github.com/mono/mono/tree/master/mcs/class/System.Configuration)


[![Build Status](https://travis-ci.org/gboucher90/system-configuration-netcore.svg?branch=master)](https://travis-ci.org/gboucher90/system-configuration-netcore)
