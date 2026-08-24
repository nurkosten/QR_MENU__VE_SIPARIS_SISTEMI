namespace RestaurantMenu.Business.Dtos;

public class SalesReportDto
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public int OrderCount { get; set; }

    public decimal TotalSales { get; set; }

    public IReadOnlyList<ProductSalesRow> TopProducts { get; set; } = [];
}

public class ProductSalesRow
{
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal Amount { get; set; }
}
