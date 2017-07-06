// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test
{
	public class InMemoryStorage : IDisposable
	{
		private static int _counter;
		private static readonly object _syncRoot = new object();
		private readonly SqliteConnection _connection;

		public InMemoryStorage()
		{
			lock (_syncRoot)
			{
				ConnectionString = $"Data Source=file:memdb{_counter}?mode=memory&cache=shared";
				//_connectionString = "Data Source=file:memdb?mode=memory&cache=shared";
				_counter++;
			}

			var connectionString = ConnectionString; //"Data Source=:memory:;";
			_connection = new SqliteConnection(connectionString);
			_connection.Open();
		}

		public string ConnectionString { get; }

		public void Dispose()
		{
			_connection.Dispose();
		}
	}

	public class InMemoryEFUserStoreTest : UserManagerTestBase<IdentityUser, IdentityRole, string>,
		IClassFixture<InMemoryStorage>
	{
		private readonly InMemoryStorage _storage;

		public InMemoryEFUserStoreTest(InMemoryStorage storage)
		{
			_storage = storage;
		}

		protected override TestConnectionFactory CreateTestContext()
		{
			var connectionString = _storage.ConnectionString;

			var factory = new TestConnectionFactory(new SQLiteDataProvider(), "InMemoryEFUserStoreTest", connectionString);
			CreateTables(factory, connectionString);
			return factory;
		}

		protected override void AddUserStore(IServiceCollection services, TestConnectionFactory context = null)
		{
			services.AddSingleton<IUserStore<IdentityUser>>(
				new UserStore<IdentityUser>(context ?? CreateTestContext()));
		}

		protected override void AddRoleStore(IServiceCollection services, TestConnectionFactory context = null)
		{
			var store = new RoleStore<IdentityRole>(context ?? CreateTestContext());
			services.AddSingleton<IRoleStore<IdentityRole>>(store);
		}

		protected override IdentityUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
			bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = default(DateTimeOffset?),
			bool useNamePrefixAsUserName = false)
		{
			return new IdentityUser
			{
				UserName = useNamePrefixAsUserName ? namePrefix : string.Format("{0}{1}", namePrefix, Guid.NewGuid()),
				Email = email,
				PhoneNumber = phoneNumber,
				LockoutEnabled = lockoutEnabled,
				LockoutEnd = lockoutEnd
			};
		}

		protected override IdentityRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
		{
			var roleName = useRoleNamePrefixAsRoleName
				? roleNamePrefix
				: string.Format("{0}{1}", roleNamePrefix, Guid.NewGuid());
			return new IdentityRole(roleName);
		}

		protected override void SetUserPasswordHash(IdentityUser user, string hashedPassword)
		{
			user.PasswordHash = hashedPassword;
		}

		protected override Expression<Func<IdentityUser, bool>> UserNameEqualsPredicate(string userName)
		{
			return u => u.UserName == userName;
		}

		protected override Expression<Func<IdentityRole, bool>> RoleNameEqualsPredicate(string roleName)
		{
			return r => r.Name == roleName;
		}

		protected override Expression<Func<IdentityUser, bool>> UserNameStartsWithPredicate(string userName)
		{
			return u => u.UserName.StartsWith(userName);
		}

		protected override Expression<Func<IdentityRole, bool>> RoleNameStartsWithPredicate(string roleName)
		{
			return r => r.Name.StartsWith(roleName);
		}
	}
}