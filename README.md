# System.Configuration for .NET Core

Compatible with NET Standard >= 1.5

- Bring back (partially) the good old [System.Configuration.ConfigurationManager](https://msdn.microsoft.com/en-us/library/system.configuration.configurationmanager(v=vs.110).aspx)
- Encryption is not supported
- Read only
- Machine & Roaming configurations are not supported
- Requires manual initialization of the ConfigurationManager to specify the path to the *.config file
- Not fully tested
- Code adapted from Mono (https://github.com/mono/mono/tree/master/mcs/class/System.Configuration)

The purpose of this library is to help migrating existing .NET projects to .NET Core.
It is not meant as a replacement, it simply provides basic features and does not maintain all the existing interfaces.

[![Build Status](https://travis-ci.org/gboucher90/system-configuration-netcore.svg?branch=master)](https://travis-ci.org/gboucher90/system-configuration-netcore)
