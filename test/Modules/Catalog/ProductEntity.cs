using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Xunit;

namespace Catalog.Domain.Tests.Entities.Products
{
    public class ProductTests
    {
        #region Test Data Helpers

        private static (Guid organizationId, CategoryId categoryId, string name, decimal price, string? barcode) ValidData()
        {
            return (
                Guid.NewGuid(),
                CategoryId.New(),
                "Gigabyte GeForce RTX 4060 TI Gaming OC 8GB",
                21500m,
                "1234567890123");
        }

        private static Product CreateValidProduct(string? barcode = null)
        {
            var data = ValidData();
            return Product.Create(data.organizationId, data.categoryId, data.name, data.price, barcode ?? data.barcode);
        }

        #endregion

        #region Create

        [Fact]
        public void Create_WithValidData_ShouldSetAllPropertiesCorrectly()
        {
            var (organizationId, categoryId, name, price, barcode) = ValidData();

            var product = Product.Create(organizationId, categoryId, name, price, barcode);

            Assert.Equal(organizationId, product.OrganizationId);
            Assert.Equal(categoryId, product.CategoryId);
            Assert.Equal(name, product.Name);
            Assert.Equal(price, product.SellingPrice);
            Assert.Equal(barcode, product.Barcode);
            Assert.True(product.IsActive);
            Assert.Null(product.ImagePath);
            Assert.NotEqual(default, product.Id);
        }

        [Fact]
        public void Create_WithoutBarcode_ShouldSetBarcodeToNull()
        {
            var (organizationId, categoryId, name, price, _) = ValidData();

            var product = Product.Create(organizationId, categoryId, name, price);

            Assert.Null(product.Barcode);
        }

        [Theory]
        [InlineData("  1234567890123  ", "1234567890123")]
        [InlineData("ABC-999", "ABC-999")]
        public void Create_WithBarcodeContainingWhitespace_ShouldTrimBarcode(string input, string expected)
        {
            var (organizationId, categoryId, name, price, _) = ValidData();

            var product = Product.Create(organizationId, categoryId, name, price, input);

            Assert.Equal(expected, product.Barcode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithNullOrWhitespaceBarcode_ShouldSetBarcodeToNull(string? barcode)
        {
            var (organizationId, categoryId, name, price, _) = ValidData();

            var product = Product.Create(organizationId, categoryId, name, price, barcode);

            Assert.Null(product.Barcode);
        }

        [Theory]
        [InlineData("  Gigabyte RTX 4060  ", "Gigabyte RTX 4060")]
        public void Create_WithNameContainingWhitespace_ShouldTrimName(string input, string expected)
        {
            var (organizationId, categoryId, _, price, barcode) = ValidData();

            var product = Product.Create(organizationId, categoryId, input, price, barcode);

            Assert.Equal(expected, product.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithNullOrWhitespaceName_ShouldThrowArgumentException(string? name)
        {
            var (organizationId, categoryId, _, price, barcode) = ValidData();

            var ex = Assert.Throws<ArgumentException>(() =>
                Product.Create(organizationId, categoryId, name!, price, barcode));

            Assert.Equal("name", ex.ParamName);
        }

        [Fact]
        public void Create_WithNegativeSellingPrice_ShouldThrowArgumentOutOfRangeException()
        {
            var (organizationId, categoryId, name, _, barcode) = ValidData();

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                Product.Create(organizationId, categoryId, name, -1m, barcode));

            Assert.Equal("sellingPrice", ex.ParamName);
        }

        [Fact]
        public void Create_WithZeroSellingPrice_ShouldBeAllowed()
        {
            var (organizationId, categoryId, name, _, barcode) = ValidData();

            var product = Product.Create(organizationId, categoryId, name, 0m, barcode);

            Assert.Equal(0m, product.SellingPrice);
        }

        [Fact]
        public void Create_CalledTwiceWithSameData_ShouldProduceDifferentIds()
        {
            var (organizationId, categoryId, name, price, barcode) = ValidData();

            var product1 = Product.Create(organizationId, categoryId, name, price, barcode);
            var product2 = Product.Create(organizationId, categoryId, name, price, barcode);

            Assert.NotEqual(product1.Id, product2.Id);
        }

        #endregion

        #region Rename

        [Fact]
        public void Rename_WithValidName_ShouldUpdateName()
        {
            var product = CreateValidProduct();

            product.Rename("New Product Name");

            Assert.Equal("New Product Name", product.Name);
        }

        [Fact]
        public void Rename_WithNameContainingLeadingTrailingWhitespace_ShouldTrimName()
        {
            var product = CreateValidProduct();

            product.Rename("   Trimmed Name   ");

            Assert.Equal("Trimmed Name", product.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Rename_WithNullOrWhitespaceName_ShouldThrowArgumentException_AndNotChangeState(string? name)
        {
            var product = CreateValidProduct();
            var originalName = product.Name;

            var ex = Assert.Throws<ArgumentException>(() => product.Rename(name!));

            Assert.Equal("name", ex.ParamName);
            Assert.Equal(originalName, product.Name);
        }

        #endregion

        #region ChangeSellingPrice

        [Fact]
        public void ChangeSellingPrice_WithValidPrice_ShouldUpdatePrice()
        {
            var product = CreateValidProduct();

            product.ChangeSellingPrice(999.99m);

            Assert.Equal(999.99m, product.SellingPrice);
        }

        [Fact]
        public void ChangeSellingPrice_WithZero_ShouldBeAllowed()
        {
            var product = CreateValidProduct();

            product.ChangeSellingPrice(0m);

            Assert.Equal(0m, product.SellingPrice);
        }

        [Fact]
        public void ChangeSellingPrice_WithNegativeValue_ShouldThrowArgumentOutOfRangeException_AndNotChangeState()
        {
            var product = CreateValidProduct();
            var originalPrice = product.SellingPrice;

            Assert.Throws<ArgumentOutOfRangeException>(() => product.ChangeSellingPrice(-0.01m));

            Assert.Equal(originalPrice, product.SellingPrice);
        }

        #endregion

        #region ChangeOrganization

        [Fact]
        public void ChangeOrganization_WithNewOrganizationId_ShouldUpdateOrganizationId()
        {
            var product = CreateValidProduct();
            var newOrganizationId = Guid.NewGuid();

            product.ChangeOrganization(newOrganizationId);

            Assert.Equal(newOrganizationId, product.OrganizationId);
        }

        #endregion

        #region ChangeCategory

        [Fact]
        public void ChangeCategory_WithNewCategoryId_ShouldUpdateCategoryId()
        {
            var product = CreateValidProduct();
            var newCategoryId = CategoryId.New();

            product.ChangeCategory(newCategoryId);

            Assert.Equal(newCategoryId, product.CategoryId);
        }

        #endregion

        #region ChangeBarcode

        [Fact]
        public void ChangeBarcode_WithValidValue_ShouldUpdateBarcode()
        {
            var product = CreateValidProduct();

            product.ChangeBarcode("9998887776665");

            Assert.Equal("9998887776665", product.Barcode);
        }

        [Fact]
        public void ChangeBarcode_WithWhitespacePaddedValue_ShouldTrimBarcode()
        {
            var product = CreateValidProduct();

            product.ChangeBarcode("  777888999  ");

            Assert.Equal("777888999", product.Barcode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ChangeBarcode_WithNullOrWhitespace_ShouldSetBarcodeToNull(string? barcode)
        {
            var product = CreateValidProduct("123456");

            product.ChangeBarcode(barcode);

            Assert.Null(product.Barcode);
        }

        #endregion

        #region ChangeImage

        [Fact]
        public void ChangeImage_WithValidPath_ShouldUpdateImagePath()
        {
            var product = CreateValidProduct();

            product.ChangeImage("/images/products/rtx-4060.png");

            Assert.Equal("/images/products/rtx-4060.png", product.ImagePath);
        }

        [Fact]
        public void ChangeImage_WithWhitespacePaddedPath_ShouldTrimPath()
        {
            var product = CreateValidProduct();

            product.ChangeImage("   /images/rtx.png   ");

            Assert.Equal("/images/rtx.png", product.ImagePath);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ChangeImage_WithNullOrWhitespace_ShouldSetImagePathToNull(string? imagePath)
        {
            var product = CreateValidProduct();
            product.ChangeImage("/images/some.png");

            product.ChangeImage(imagePath);

            Assert.Null(product.ImagePath);
        }

        #endregion

        #region Activate / Deactivate

        [Fact]
        public void Product_WhenCreated_ShouldBeActiveByDefault()
        {
            var product = CreateValidProduct();

            Assert.True(product.IsActive);
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            var product = CreateValidProduct();

            product.Deactivate();

            Assert.False(product.IsActive);
        }

        [Fact]
        public void Activate_AfterDeactivate_ShouldSetIsActiveToTrue()
        {
            var product = CreateValidProduct();
            product.Deactivate();

            product.Activate();

            Assert.True(product.IsActive);
        }

        [Fact]
        public void Deactivate_CalledTwice_ShouldRemainFalse_Idempotent()
        {
            var product = CreateValidProduct();

            product.Deactivate();
            product.Deactivate();

            Assert.False(product.IsActive);
        }

        [Fact]
        public void Activate_CalledTwice_ShouldRemainTrue_Idempotent()
        {
            var product = CreateValidProduct();

            product.Activate();
            product.Activate();

            Assert.True(product.IsActive);
        }

        #endregion

        #region Combined / Integration Scenarios

        [Fact]
        public void MultipleStateChanges_ShouldAllPersistIndependently()
        {
            var product = CreateValidProduct();

            var newOrganizationId = Guid.NewGuid();
            var newCategoryId = CategoryId.New();

            product.ChangeOrganization(newOrganizationId);
            product.ChangeCategory(newCategoryId);
            product.Rename("Updated Name");
            product.ChangeSellingPrice(500m);
            product.ChangeBarcode("NEWBARCODE");
            product.ChangeImage("/img/new.png");
            product.Deactivate();

            Assert.Equal(newOrganizationId, product.OrganizationId);
            Assert.Equal(newCategoryId, product.CategoryId);
            Assert.Equal("Updated Name", product.Name);
            Assert.Equal(500m, product.SellingPrice);
            Assert.Equal("NEWBARCODE", product.Barcode);
            Assert.Equal("/img/new.png", product.ImagePath);
            Assert.False(product.IsActive);
        }

        [Fact]
        public void FailedRename_ShouldNotAffectOtherProperties()
        {
            var product = CreateValidProduct();
            var originalPrice = product.SellingPrice;
            var originalOrganizationId = product.OrganizationId;

            Assert.Throws<ArgumentException>(() => product.Rename(""));

            Assert.Equal(originalPrice, product.SellingPrice);
            Assert.Equal(originalOrganizationId, product.OrganizationId);
        }

        #endregion
    }
}