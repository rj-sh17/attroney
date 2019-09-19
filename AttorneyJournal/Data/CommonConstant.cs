using System;

namespace AttorneyJournal.Data
{
	public static class CommonConstant
	{
		public const string UserRole = "User";
		public const string AttorneyRole = "Attorney";
		public const string AdministratorRole = "Administrator";

		public const string ClaimName = "Name";
		public const string ClaimSurname = "Surname";

#if DEBUG

		public static Guid DefaultAttorneyId = new Guid("58CEAFA0-9AD9-468D-A16B-4EC8F2944D6F");
		public const string DefaultAttorneyUserMail = "dev+attorney@macoev.com";
		public const string DefaultAttorneyPassword = "Prova_2017";
		public const string DefaultAttorneyName = "John";
		public const string DefaultAttorneySurname = "Doe";

		public const string SuperAdminUserMail = "dev+sa@macoev.com";
		public const string SuperAdminPassword = "Prova_2017";
		public const string SuperAdminName = "John";
		public const string SuperAdminSurname = "Doe";

#else
		public static Guid DefaultAttorneyId = new Guid("4D8EF8A6-5D24-468D-8B53-F29BC45A20FC");
		public const string DefaultAttorneyUserMail = "services@aires.io";
		public const string DefaultAttorneyPassword = "VER-Aires2017!";
		public const string DefaultAttorneyName = "Daniel";
		public const string DefaultAttorneySurname = "Provder";

		public const string SuperAdminUserMail = "services+sa@aires.io";
		public const string SuperAdminPassword = "VER-Aires2017!";
		public const string SuperAdminName = "Daniel";
		public const string SuperAdminSurname = "Provder";

#endif
	}
}
