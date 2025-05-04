# Introduction
Purpose of this project is to show how will look AutoMapper profiles replaced by manual classes mapping. 
Using this app you can easily see how "good" and "reliable" is the mapping done automatically by AutoMapper library.
Probably your AutoMapper mappings contains a lot of suspicious types conversions and you even not aware of it.
This small console application uses Microsoft.CodeAnalysis to search project AutoMapper profiles and try to generate classes that do mapping manually.
Then app generates a class that contains mapping functions for each profile found. 
The generated class is saved in the specified output directory.

Second scenario is to generate mapping functions for any two types in your solution.

Path to the project to analyze is taken from the command line argument.
If types on the source and destination are the same, it will be a simple assignment of the value.
If the types are different, it will be a conversion function call.
If the conversion function is not found in configuration, it will be a Unknown function called.
If a lot of properties assignment is done by convert function it is a sign of potentially problems with app.

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
- SearchClassPostfixes - List of class postfixes to search in symbols. 
- DefaultNameSpaces - List of default name spaces to add in the generated classes.
- Collections - List of collections recognized by app.
- ConvertFunctions - List of conversion functions

Example: 

AutoMapperMigratorConsole.exe TestAutomapperSolution.sln

Profile in test solution:
```csharp
        CreateMap<Place, AddressResponse>()
            .ForMember(dest => dest.CityName, opt => opt.MapFrom(src => src.AddressCity))
            .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.AddressStreet))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.AddressCountryCodeIso))
            .ForMember(dest => dest.FlatNo, opt => opt.MapFrom(src => src.AddressNo2))
            .ForMember(dest => dest.HouseNo, opt => opt.MapFrom(src => src.AddressNo1))
            .ForMember(dest => dest.PostCode, opt => opt.MapFrom(src => src.PostCode))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.AddressCountryCodeIso))
            .ForMember(dest => dest.Teryt, opt => opt.Ignore())
            .ForMember(dest => dest.GeoLocalizationData, opt => opt.Ignore())
            .AfterMap((src, dst) =>
            {
                var con = new List<Contact>();
                con.Add(new Contact() { ContactType = ContactTypeEnum.PHONE.ToString(), Value = src.Phone });
                con.Add(new Contact() { ContactType = ContactTypeEnum.EMAIL.ToString(), Value = src.ContactMail });
                dst.ContactList = con;
            });

        CreateMap<Place, ProviderInfo>()
            .ForMember(dest => dest.EntityId, opt => opt.MapFrom(src => src.ParentPlaceId))
            .ForMember(dest => dest.ProviderId, opt => opt.MapFrom(src => src.PlaceId))
            .ForMember(dest => dest.ProviderName, opt => opt.MapFrom(src => src.AlternativeName))
            .ForMember(dest => dest.ExternalCode, opt => opt.MapFrom(src => src.ExternalCode))
            .ForMember(dest => dest.Rcode, opt => opt.MapFrom(src => src.ExternalCode))
            .ForMember(dest => dest.ResortCodeV, opt => opt.Ignore())
            .ForMember(dest => dest.ResortCodeVII, opt => opt.MapFrom(src => src.NationalId1))
            .ForMember(dest => dest.ResortCodeVIII, opt => opt.MapFrom(src => src.NationalId2))
            .ForMember(dest => dest.StaffData, opt => opt.Ignore())
            .ForMember(dest => dest.AdressData, opt => opt.MapFrom(src => src))
            ;
```
Output:

```csharp
    public static AddressResponse MapAddressResponse(Place source)
    {
        var desc = new AddressResponse();
        desc.Street = source.AddressStreet;
        desc.CityName = source.AddressCity;
        desc.HouseNo = source.AddressNo1;
        desc.FlatNo = source.AddressNo2;
        desc.PostCode = source.PostCode;
        desc.Country = source.AddressCountryCodeIso;
        {
            var con = new List<Contact>();
            con.Add(new Contact() { ContactType = ContactTypeEnum.PHONE.ToString(), Value = source.Phone });
            con.Add(new Contact() { ContactType = ContactTypeEnum.EMAIL.ToString(), Value = source.ContactMail });
            desc.ContactList = con;
        }

        return desc;
    }

    public static ProviderInfo MapProviderInfo(Place source)
    {
        var desc = new ProviderInfo();
        desc.EntityId = source.ParentPlaceId;
        desc.ProviderId = source.PlaceId;
        desc.ProviderName = source.AlternativeName;
        desc.ExternalCode = source.ExternalCode;
        desc.Rcode = source.ExternalCode;
        desc.ResortCodeVII = source.NationalId1;
        desc.ResortCodeVIII = source.NationalId2;
        desc.AdressData = MapAddressResponse(source);
        return desc;
    }
```

Example any two types mapping (with all child types):

AutoMapperMigratorConsole.exe TestSolution.sln ClassA ClassARequest

```csharp
public class ClassA
{
    public byte[] Id { get; set; }

    public string Name { get; set; }

    public string Surname { get; set; }

    public string DocumentType { get; set; }

    public string DocumentValue { get; set; }

    public List<string> Specialization { get; set; }

    public ClassB ExtraData { get; set; }

    public List<ClassC> ClassCs { get; set; }
}

public class ClassARequest
{
    public byte[] Id { get; set; }

    public string Name { get; set; }

    public string Surname { get; set; }

    public string DocumentType { get; set; }

    public string DocumentValue { get; set; }

    public ClassBRequest ExtraData { get; set; }

    public IList<ClassCRequest> ClassCs { get; set; }

    public List<string> AbaSpecialization { get; set; }
}
```
Output:

```csharp
    public static ClassBRequest MapClassBRequest(ClassB source)
    {
        var desc = new ClassBRequest();
        desc.P1 = source.P1;
        desc.P2 = source.P2;
        desc.EnumList = source.EnumList != null ? source.EnumList.Select(CastEnum<EnumB, EnumBRequest>).ToList() : new List<EnumBRequest>();
        return desc;
    }

    public static ClassCRequest MapClassCRequest(ClassC source)
    {
        var desc = new ClassCRequest();
        desc.X = source.X;
        desc.Y = source.Y;
        return desc;
    }

    public static ClassARequest MapClassARequest(ClassA source)
    {
        var desc = new ClassARequest();
        desc.Id = source.Id.Cast<byte>().ToArray();
        desc.Name = source.Name;
        desc.Surname = source.Surname;
        desc.DocumentType = source.DocumentType;
        desc.DocumentValue = source.DocumentValue;
        desc.ExtraData = MapClassBRequest(source.ExtraData);
        desc.ClassCs = source.ClassCs != null ? source.ClassCs.Select(MapClassCRequest).ToList() : new List<ClassCRequest>();
        desc.AbaSpecialization = source.Specialization != null ? source.Specialization.ToList() : new List<string>();
        return desc;
    }
```