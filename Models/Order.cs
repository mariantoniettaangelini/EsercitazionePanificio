namespace Esercitazione.Models
{
    public class Order
    {
        public int OrderId  { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string CustomerNotes { get; set; } = "";
        public bool Completed { get; set; }
        public bool IsInCart { get; set; } = true;
    }
}
