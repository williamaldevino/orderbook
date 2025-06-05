using System.ComponentModel.DataAnnotations;

namespace OrderBookApi;

public class PlaceOrder
{
    [Required]
    [StringLength(maximumLength: 50, MinimumLength = 1, ErrorMessage = "The Wallet Code must be between 1 and 50 characters")]
    public required string WalletCode { get; init; } 
   
    [Required]
    [Range(0.00000001, double.MaxValue, ErrorMessage = "Price must be positive.")]
    public decimal Price { get; init; }
    [Required]
    [Range(0.00000001, double.MaxValue, ErrorMessage = "Quantity must be positive.")]
    public int Quantity { get; init; }
}