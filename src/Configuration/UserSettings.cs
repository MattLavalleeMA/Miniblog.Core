// Copyright (c) 2019 All rights reserved.
// Code should follow the .NET Standard Guidelines:
// https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/

using System.Collections.Generic;

namespace Miniblog.Core.Configuration
{
    /// <summary>
    /// Defines the <see cref="UserSettings" />
    /// </summary>
    public class UserSettings
    {
        /// <summary>
        /// Gets or sets the Salt
        /// </summary>
        public string Salt { get; set; }

        /// <summary>
        /// Gets or sets the List of Users
        /// </summary>
        public List<User> Users { get; set; }
    }

    public class User
    {
        /// <summary>
        /// Gets or sets the UserId
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the FirstName
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the LastName
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the Nickname
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// Gets or sets the Email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the Url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the Password
        /// </summary>
        public string Password { get; set; }
    }
}
