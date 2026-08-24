using RestaurantMenu.Entities.Enums;

namespace RestaurantMenu.WebUI.Infrastructure;

public static class DisplayTexts
{
    public static string OrderStatus(OrderStatus status) => status switch
    {
        Entities.Enums.OrderStatus.New => "Yeni Sipariş",
        Entities.Enums.OrderStatus.Confirmed => "Onaylandı",
        Entities.Enums.OrderStatus.Preparing => "Hazırlanıyor",
        Entities.Enums.OrderStatus.Ready => "Hazır",
        Entities.Enums.OrderStatus.Served => "Servis Edildi",
        Entities.Enums.OrderStatus.Completed => "Tamamlandı",
        Entities.Enums.OrderStatus.Cancelled => "İptal Edildi",
        _ => status.ToString()
    };

    public static string StatusBadge(OrderStatus status) => status switch
    {
        Entities.Enums.OrderStatus.New => "warning",
        Entities.Enums.OrderStatus.Confirmed => "info",
        Entities.Enums.OrderStatus.Preparing => "primary",
        Entities.Enums.OrderStatus.Ready => "success",
        Entities.Enums.OrderStatus.Served => "secondary",
        Entities.Enums.OrderStatus.Completed => "dark",
        Entities.Enums.OrderStatus.Cancelled => "danger",
        _ => "light"
    };

    public static string ServiceType(ServiceRequestType type) =>
        type == ServiceRequestType.CallWaiter ? "Garson Çağır" : "Hesap İste";

    public static string ServiceStatus(ServiceRequestStatus status) => status switch
    {
        ServiceRequestStatus.Pending => "Bekliyor",
        ServiceRequestStatus.Acknowledged => "Alındı",
        ServiceRequestStatus.Completed => "Tamamlandı",
        _ => status.ToString()
    };
}
