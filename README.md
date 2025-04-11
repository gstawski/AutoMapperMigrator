# Introduction
Purpose of this project is to show how will look AutoMapper profiles replaced by manual classes mapping. 
Using this app you can easily see how "good" and "reliable" is the mapping done automatically by AutoMapper library.
Probably your AutoMapper mappings contains a lot of suspicious types conversions and you even not aware of it.
This small console application uses Microsoft.CodeAnalysis to search project AutoMapper profiles and try to generate classes that do mapping manually.
Then app generates a class that contains mapping functions for each profile found. 
The generated class is saved in the specified output directory.

Path to the project to analyze is taken from the command line argument.
If types on the source and destination are the same, it will be a simple assignment of the value.
If the types are different, it will be a conversion function call.
If the conversion function is not found in configuration, it will be a Unknown function called.
If a lot of properties assignment is done by convert function is is a sign of potentially problems with app.

# How to use
AutoMapperMigratorConsole.exe c:\path\to\project\to\analyze\solution.sln

# Output
The output of the app is displayed in the console and saved in the specified output directory.

# App Configuration
ConvertFunctionsConfiguration.xml File contains the configuration for the conversion.
It contains the following sections:
- OutputDirectoryPath - Directory where map class where created.
- OutputFileName - File name of the generated class.
- MapperClassName - Name of the class.
- MapFunctionNamesPrefix - Prefix for the generated mapping functions.
- UseFullNameSpaces - Use full name spaces for the generated classes.
- DefaultNameSpaces - List of default name spaces to add in the generated classes.
- Collections - List of collections recognized by app.
- ConvertFunctions - List of conversion functions
