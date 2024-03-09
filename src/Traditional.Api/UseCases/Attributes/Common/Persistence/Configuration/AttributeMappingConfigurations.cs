using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Entities;

namespace Traditional.Api.UseCases.Attributes.Common.Persistence.Configuration;

/// <inheritdoc />
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "It is more readable if the seeding data is at the end of the class")]
internal class AttributeMappingConfigurations : IEntityTypeConfiguration<AttributeMapping>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AttributeMapping> builder)
    {
        builder
            .HasKey(attributeMapping => attributeMapping.Id);

        builder
            .HasIndex(attributeMapping => attributeMapping.AttributeReference)
            .IsUnique();

        builder.HasData(GetSeedingData());
    }

    /// <summary>
    /// Provides the seeding data for the attribute mappings.
    /// </summary>
    /// <returns>An array of attribute mappings.</returns>
    public static AttributeMapping[] GetSeedingData()
    {
        const string heightMapping = "Height";
        const string widthMapping = "Width";
        const string lengthMapping = "Length";
        const string centimetersMapping = "centimeters";

        return
        [
            new AttributeMapping("condition_type,value", "new_new") { Id = 1 },
            new AttributeMapping("externally_assigned_product_identifier,type", "ean") { Id = 2 },
            new AttributeMapping("manufacturer,value", "CompanyName") { Id = 3 },
            new AttributeMapping("brand,value", "CompanyName") { Id = 4 },
            new AttributeMapping("country_of_origin,value", "DE") { Id = 5 },
            new AttributeMapping("item_package_weight,unit", "kilograms") { Id = 6 },
            new AttributeMapping("item_weight,unit", "kilograms") { Id = 7 },
            new AttributeMapping("item_package_dimensions,length,unit", centimetersMapping) { Id = 8 },
            new AttributeMapping("item_package_dimensions,height,unit", centimetersMapping) { Id = 9 },
            new AttributeMapping("item_package_dimensions,width,unit", centimetersMapping) { Id = 10 },
            new AttributeMapping("item_dimensions,length,unit", centimetersMapping) { Id = 11 },
            new AttributeMapping("item_dimensions,height,unit", centimetersMapping) { Id = 12 },
            new AttributeMapping("item_dimensions,width,unit", centimetersMapping) { Id = 13 },
            new AttributeMapping("supplier_declared_dg_hz_regulation,value", "not_applicable") { Id = 14 },
            new AttributeMapping("supplier_declared_material_regulation,value", "not_applicable") { Id = 15 },
            new AttributeMapping("item_name,value", "TitleMarketplace") { Id = 16 },
            new AttributeMapping("item_type_name,value", "TitleMarketplace") { Id = 17 },
            new AttributeMapping("item_package_weight,value", "SingleItemPackageWeight") { Id = 18 },
            new AttributeMapping("item_weight,value", "Weight") { Id = 19 },
            new AttributeMapping("item_package_dimensions,length,value", "SingleItemPackageLength") { Id = 20 },
            new AttributeMapping("item_package_dimensions,height,value", "SingleItemPackageHeight") { Id = 21 },
            new AttributeMapping("item_package_dimensions,width,value", "SingleItemPackageWidth") { Id = 22 },
            new AttributeMapping("item_dimensions,length,value", lengthMapping) { Id = 23 },
            new AttributeMapping("item_dimensions,height,value", heightMapping) { Id = 24 },
            new AttributeMapping("item_dimensions,width,value", widthMapping) { Id = 25 },
            new AttributeMapping("product_description,value", "DescriptionLongMarketplaces") { Id = 26 },
            new AttributeMapping("bullet_point,value1", "MarketplaceBulletPoint1") { Id = 27 },
            new AttributeMapping("bullet_point,value2", "MarketplaceBulletPoint2") { Id = 28 },
            new AttributeMapping("bullet_point,value3", "MarketplaceBulletPoint3") { Id = 29 },
            new AttributeMapping("bullet_point,value4", "MarketplaceBulletPoint4") { Id = 30 },
            new AttributeMapping("bullet_point,value5", "MarketplaceBulletPoint5") { Id = 31 },
            new AttributeMapping("is_fragile,value", "Fragile") { Id = 32 },
            new AttributeMapping("mob", "SingleItemPreferredPackaging") { Id = 33 },
            new AttributeMapping("power_plug_type,value", "PowerPlug") { Id = 34 },
            new AttributeMapping("color,value", "Colors") { Id = 35 },
            new AttributeMapping("list_price,currency", "EUR") { Id = 36 },
            new AttributeMapping("generic_keyword,value", "SearchTermsMarketplace") { Id = 37 },
            new AttributeMapping("item_depth_width_height,depth,unit", centimetersMapping) { Id = 38 },
            new AttributeMapping("item_depth_width_height,height,unit", centimetersMapping) { Id = 39 },
            new AttributeMapping("item_depth_width_height,width,unit", centimetersMapping) { Id = 40 },
            new AttributeMapping("item_depth_width_height,depth,value", lengthMapping) { Id = 41 },
            new AttributeMapping("item_depth_width_height,height,value", heightMapping) { Id = 42 },
            new AttributeMapping("item_depth_width_height,width,value", widthMapping) { Id = 43 },
            new AttributeMapping("number_of_boxes,value", "1") { Id = 44 },
            new AttributeMapping("recommended_browse_nodes,value") { Id = 45 },
            new AttributeMapping("purchase_api_materials", "customerMaterials") { Id = 46 },
            new AttributeMapping("material_percentages", "materialPercentage") { Id = 47 },
            new AttributeMapping("images", "ImageUrl") { Id = 48 },
            new AttributeMapping("unit_count,value", "1") { Id = 49 },
            new AttributeMapping("unit_count,type,value") { Id = 50 },
            new AttributeMapping("material,value") { Id = 51 },
            new AttributeMapping("batteries_required,value", "EnergyStorageModelBattery") { Id = 52 },
            new AttributeMapping("battery_type_accumulator", "EnergyStorageModelAccumulator") { Id = 53 },
            new AttributeMapping("batteries_included,value", "EnergyStorageAmount") { Id = 54 },
            new AttributeMapping("parent_item_name,value", "parent_TitleMarketplace") { Id = 55 },
            new AttributeMapping("parent_bullet_point,value1", "parent_MarketplaceBulletPoint1") { Id = 56 },
            new AttributeMapping("parent_bullet_point,value2", "parent_MarketplaceBulletPoint2") { Id = 57 },
            new AttributeMapping("parent_bullet_point,value3", "parent_MarketplaceBulletPoint3") { Id = 58 },
            new AttributeMapping("parent_bullet_point,value4", "parent_MarketplaceBulletPoint4") { Id = 59 },
            new AttributeMapping("parent_bullet_point,value5", "parent_MarketplaceBulletPoint5") { Id = 60 },
            new AttributeMapping("parent_product_description,value", "parent_DescriptionLongMarketplaces") { Id = 61 },
            new AttributeMapping("color,standardized_values") { Id = 62 },
            new AttributeMapping("is_expiration_dated_product,value", "false") { Id = 63 },
            new AttributeMapping("deprecated_offering_start_date,value") { Id = 64 },
            new AttributeMapping("model_number,value") { Id = 65 },
            new AttributeMapping("model_name,value", "TitleShop") { Id = 66 },
            new AttributeMapping("included_components,value", "DeliveryContentsText") { Id = 67 },
            new AttributeMapping("department,value", "CompanyName") { Id = 68 },
            new AttributeMapping("item_width_height,width,value", widthMapping) { Id = 69 },
            new AttributeMapping("item_width_height,height,value", heightMapping) { Id = 70 },
            new AttributeMapping("item_length_width,width,value", widthMapping) { Id = 71 },
            new AttributeMapping("item_length_width,length,value", lengthMapping) { Id = 72 },
            new AttributeMapping("item_length_width_height,height,value", heightMapping) { Id = 73 },
            new AttributeMapping("item_length_width_height,length,value", lengthMapping) { Id = 74 },
            new AttributeMapping("item_length_width_height,width,value", widthMapping) { Id = 75 },
            new AttributeMapping("item_length_width_thickness,thickness,value", heightMapping) { Id = 76 },
            new AttributeMapping("item_length_width_thickness,length,value", lengthMapping) { Id = 77 },
            new AttributeMapping("item_length_width_thickness,width,value", widthMapping) { Id = 78 },
            new AttributeMapping("item_width_height,width,unit", centimetersMapping) { Id = 79 },
            new AttributeMapping("item_width_height,height,unit", centimetersMapping) { Id = 80 },
            new AttributeMapping("item_length_width,width,unit", centimetersMapping) { Id = 81 },
            new AttributeMapping("item_length_width,length,unit", centimetersMapping) { Id = 82 },
            new AttributeMapping("item_length_width_height,height,unit", centimetersMapping) { Id = 83 },
            new AttributeMapping("item_length_width_height,length,unit", centimetersMapping) { Id = 84 },
            new AttributeMapping("item_length_width_height,width,unit", centimetersMapping) { Id = 85 },
            new AttributeMapping("item_length_width_thickness,thickness,unit", centimetersMapping) { Id = 86 },
            new AttributeMapping("item_length_width_thickness,length,unit", centimetersMapping) { Id = 87 },
            new AttributeMapping("item_length_width_thickness,width,unit", centimetersMapping) { Id = 88 },
            new AttributeMapping("contains_food_or_beverage,value", "false") { Id = 89 },
            new AttributeMapping("is_oem_authorized,value", "false") { Id = 90 },
            new AttributeMapping("product_expiration_type,value", "Does Not Expire") { Id = 91 },
            new AttributeMapping("warranty_description,value", "-") { Id = 92 },
            new AttributeMapping("list_price,value_with_tax", "0") { Id = 93 }
        ];
    }
}
