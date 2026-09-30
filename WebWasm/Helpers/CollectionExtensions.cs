using WebWasm.Models;

namespace WebWasm.Helpers;

public static class CollectionExtensions
{
	extension<T>(HashSet<T> set)
	{
		/// <summary>Adds the item if absent, removes it if present (expand/collapse, select/deselect).</summary>
		public void Toggle(T item)
		{
			if (!set.Remove(item))
			{
				set.Add(item);
			}
		}
	}

	extension((Company Company, Driver Driver)[] driversWithCompany)
	{
		/// <summary>Company the driver works for, or null when the driver is not in the list.</summary>
		public Company? CompanyOf(Guid driverId) =>
			driversWithCompany.FirstOrDefault(dc => dc.Driver.Id == driverId).Company;
	}

	extension(User? user)
	{
		/// <summary>"First Last", the login when there is no first name, "Anonymous" when there is no user.</summary>
		public string ShortName => user is null
			? "Anonymous"
			: string.IsNullOrEmpty(user.UserInfo.FirstName)
				? user.Login
				: $"{user.UserInfo.FirstName} {user.UserInfo.LastName}";
	}
}
