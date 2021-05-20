using System;
using System.Collections.Generic;
using System.Text;

namespace PromoCodeFactory.Core.Domain.PromoCodeManagement
{
	public class Customer
		: BaseEntity
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }

		public string FullName => $"{FirstName} {LastName}";

		public string Email { get; set; }

		public virtual ICollection<CustomerPreference> Preferences { get; set; }
	}
}
