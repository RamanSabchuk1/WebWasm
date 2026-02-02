using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Orders(ToastService toastService)
{
    private int totalOrders = 0;
    private int pendingOrders = 0;
    private int completedOrders = 0;

    private void AddOrder()
    {
        totalOrders++;
        pendingOrders++;
        completedOrders++;

        toastService.ShowSuccess($"Order:{totalOrders} added successfully!");
    }
}
