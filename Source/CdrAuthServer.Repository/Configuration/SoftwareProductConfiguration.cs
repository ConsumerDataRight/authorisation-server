namespace CdrAuthServer.Repository.Configuration
{
    using CdrAuthServer.Repository.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using static CdrAuthServer.Domain.Constants;

    internal class SoftwareProductConfiguration : IEntityTypeConfiguration<SoftwareProduct>
    {
        private const string _cdrRegister = "cdr-register";
        private const string _dummyUrl = "https://cdrsandbox.gov.au/logo192.png";
        public void Configure(EntityTypeBuilder<SoftwareProduct> builder)
        {
            builder.HasKey(x => x.SoftwareProductId);

            //seed software products
            builder.HasData(
                new SoftwareProduct()
                {
                    SoftwareProductId = "c6327f87-687a-4369-99a4-eaacd3bb8210",
                    SoftwareProductName = "Mock Data Recipient Software Product",
                    SoftwareProductDescription = "Mock Data Recipient Software Product",
                    LogoUri = _dummyUrl,
                    Status = EntityStatus.Active,
                    LegalEntityId = "18B75A76-5821-4C9E-B465-4709291CF0F4",
                    LegalEntityName = "Mock Data Recipient Legal Entity Name",
                    LegalEntityStatus = EntityStatus.Active,
                    BrandId = "FFB1C8BA-279E-44D8-96F0-1BC34A6B436F",
                    BrandName = "Mock Data Recipient Brand Name",
                    BrandStatus = EntityStatus.Active
                },
                new SoftwareProduct()
                {
                    SoftwareProductId = "22222222-2222-2222-2222-222222222222",
                    SoftwareProductName = "Active Data Recipient Software Product",
                    SoftwareProductDescription = "Active Data Recipient Software Product",
                    LogoUri = _dummyUrl,
                    Status = EntityStatus.Active,
                    LegalEntityId = "LLLLLLLL-2222-2222-2222-222222222222",
                    LegalEntityName = "Active Data Recipient Legal Entity Name",
                    LegalEntityStatus = EntityStatus.Active,
                    BrandId = "BBBBBBBB-2222-2222-2222-222222222222",
                    BrandName = "Active Data Recipient Brand Name",
                    BrandStatus = EntityStatus.Active,
                },
                new SoftwareProduct()
                {
                    SoftwareProductId = "99999999-9999-9999-9999-999999999999",
                    SoftwareProductName = "Removed Software Product",
                    SoftwareProductDescription = "Removed Software Product",
                    LogoUri = _dummyUrl,
                    Status = EntityStatus.Removed,
                    LegalEntityId = "LLLLLLLL-2222-2222-2222-222222222222",
                    LegalEntityName = "Active Data Recipient Legal Entity Name",
                    LegalEntityStatus = EntityStatus.Active,
                    BrandId = "BBBBBBBB-2222-2222-2222-222222222222",
                    BrandName = "Active Data Recipient Brand Name",
                    BrandStatus = EntityStatus.Active
                },
                //This is the software product with which the register is associated.
                //we need this for the GetRecipients function app to be able to fetch the access token.
                new SoftwareProduct()
                {
                    SoftwareProductId = _cdrRegister,
                    SoftwareProductName = _cdrRegister,
                    SoftwareProductDescription = "Mock Register",
                    LogoUri = _dummyUrl,
                    Status = EntityStatus.Active,
                    LegalEntityId = _cdrRegister,
                    LegalEntityName = _cdrRegister,
                    LegalEntityStatus = EntityStatus.Active,
                    BrandId = _cdrRegister,
                    BrandName = _cdrRegister,
                    BrandStatus = EntityStatus.Active
                });
        }
    }
}
