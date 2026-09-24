using System.Collections.Generic;
using Newtonsoft.Json;

namespace API_EXAMPLE
{
    //typed order payload: keeps the save path statically bound, a dynamic call on the stack
    //breaks XDCrypt.GetSignatureHash (it reads DeclaringType of every stack frame)
    public class OrderData
    {
        [JsonProperty("documentType")]
        public string DocumentType { get; set; }

        [JsonProperty("order")]
        public OrderHeader Order { get; set; }

        [JsonProperty("nr_order_lines")]
        public int NrOrderLines { get; set; }

        [JsonProperty("order_lines")]
        public List<OrderLine> OrderLines { get; set; }
    }

    public class OrderHeader
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("idUser")]
        public string IdUser { get; set; }

        [JsonProperty("total")]
        public decimal Total { get; set; }

        //tax included
        [JsonProperty("shipping_value")]
        public decimal ShippingValue { get; set; }
    }

    public class OrderLine
    {
        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("product_value")]
        public decimal ProductValue { get; set; }
    }
}
