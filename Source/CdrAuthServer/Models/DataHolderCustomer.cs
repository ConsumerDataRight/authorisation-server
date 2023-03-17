namespace CdrAuthServer.Models
{
    public class DataHolderCustomer
    {
        public List<Customer> Customers { get; set; }
    }

    public class Customer
    {        
        public string LoginId { get; set; }
        public Person Person { get; set; }        
    }

    public class Person
    {
        
        public string FirstName { get; set; }
        public string LastName { get; set; }        
    }
}
