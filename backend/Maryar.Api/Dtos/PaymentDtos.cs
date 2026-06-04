namespace Maryar.Api.Dtos
{
    public class InfinitePayPixResult
    {
        public string ChargeId { get; set; }
        public string Status { get; set; }
        public string QrCode { get; set; }       // base64 ou URL conforme provedor
        public string CopyPaste { get; set; }    // BR Code
    }

    public class InfinitePayCardResult
    {
        public string ChargeId { get; set; }
        public string Status { get; set; }       // approved | declined | pending
        public string Message { get; set; }
    }
}
