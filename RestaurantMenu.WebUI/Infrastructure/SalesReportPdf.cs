using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestaurantMenu.Business.Dtos;

namespace RestaurantMenu.WebUI.Infrastructure;

public static class SalesReportPdf
{
    public static byte[] Create(string restaurantName, string rangeLabel, SalesReportDto report)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var average = report.OrderCount > 0 ? report.TotalSales / report.OrderCount : 0m;
        var from = ToLocal(report.From);
        var to = ToLocal(report.To);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor("#1a2332"));

                page.Header().Background("#1a2332").Padding(16).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("QR Menü").FontColor("#9aa4b2").FontSize(9);
                        col.Item().Text(restaurantName).FontColor("#ffffff").FontSize(18).SemiBold();
                        col.Item().Text("Satış raporu").FontColor("#c5ccd6").FontSize(11);
                    });
                    row.ConstantItem(180).AlignRight().Column(col =>
                    {
                        col.Item().Text(rangeLabel).FontColor("#ffffff").FontSize(11).SemiBold().AlignRight();
                        col.Item().Text($"{from:dd.MM.yyyy HH:mm} – {to:dd.MM.yyyy HH:mm}")
                            .FontColor("#9aa4b2").FontSize(8).AlignRight();
                    });
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(16);

                    col.Item().Row(row =>
                    {
                        AddStat(row, "Sipariş adedi", report.OrderCount.ToString("N0", culture));
                        row.ConstantItem(10);
                        AddStat(row, "Toplam ciro", report.TotalSales.ToString("C", culture));
                        row.ConstantItem(10);
                        AddStat(row, "Ortalama sepet", average.ToString("C", culture));
                    });

                    col.Item().Text("En çok satan ürünler").FontSize(12).SemiBold();

                    if (report.TopProducts.Count == 0)
                    {
                        col.Item().Border(1).BorderColor("#e5e7eb").Padding(16)
                            .Text("Bu dönem için tamamlanmış satış bulunmuyor.").FontColor("#6b7280");
                    }
                    else
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#f3f4f6").Padding(8).Text("Ürün").SemiBold();
                                header.Cell().Background("#f3f4f6").Padding(8).AlignRight().Text("Adet").SemiBold();
                                header.Cell().Background("#f3f4f6").Padding(8).AlignRight().Text("Tutar").SemiBold();
                            });

                            foreach (var product in report.TopProducts)
                            {
                                table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(8).Text(product.ProductName);
                                table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(8).AlignRight()
                                    .Text(product.Quantity.ToString("N0", culture));
                                table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(8).AlignRight()
                                    .Text(product.Amount.ToString("C", culture));
                            }
                        });
                    }
                });

                page.Footer().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem()
                        .Text("Yalnızca seçili restoran verileri · İptal siparişler hariç")
                        .FontSize(8).FontColor("#6b7280");
                    row.ConstantItem(140).AlignRight().Text(text =>
                    {
                        text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm", culture)).FontSize(8).FontColor("#6b7280");
                        text.Span("  ");
                        text.CurrentPageNumber().FontSize(8).FontColor("#6b7280");
                        text.Span(" / ").FontSize(8).FontColor("#6b7280");
                        text.TotalPages().FontSize(8).FontColor("#6b7280");
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void AddStat(RowDescriptor row, string label, string value)
    {
        row.RelativeItem().Border(1).BorderColor("#e5e7eb").Padding(12).Column(col =>
        {
            col.Item().Text(label).FontSize(8).FontColor("#6b7280");
            col.Item().Text(value).FontSize(14).SemiBold();
        });
    }

    private static DateTime ToLocal(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
}
