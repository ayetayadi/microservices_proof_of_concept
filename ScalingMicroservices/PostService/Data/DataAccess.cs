using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PostService.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PostService.Data
{
    public class DataAccess
    { 
        // List to store multiple connection strings
        private readonly List<string> _connectionStrings = new List<string>();

        // Constructor that reads connection strings from configuration
        public DataAccess(IConfiguration configuration)
        {
            // Get connection strings section from configuration
            var connectionStrings = configuration.GetSection("PostDbConnectionStrings");
            // Loop through each connection string and add it to the list

            foreach (var connectionString in connectionStrings.GetChildren())
            {
                Console.WriteLine("ConnectionString: " + connectionString.Value);
                _connectionStrings.Add(connectionString.Value);
            }
        }

        // Method to read the latest posts based on category and count

        public async Task<ActionResult<IEnumerable<Post>>> ReadLatestPosts(string category, int count)
        {
            using var dbContext = new PostServiceContext(GetConnectionString(category));
            // Fetch latest posts from the specified category, including related user information
            return await dbContext.Post.OrderByDescending(p => p.PostId).Take(count).Include(x => x.User).Where(p => p.CategoryId == category).ToListAsync();
        }


        // Method to create a new post
        public async Task<int> CreatePost(Post post)
        {
            // Create a context with the appropriate connection string
            using var dbContext = new PostServiceContext(GetConnectionString(post.CategoryId));
            dbContext.Post.Add(post);
            return await dbContext.SaveChangesAsync();
        }

        // Method to initialize the database with users and categories
        public void InitDatabase(int countUsers, int countCategories)
        {
            // Loop through each connection string
            foreach (var connectionString in _connectionStrings)
            {
                using var dbContext = new PostServiceContext(connectionString);
                // Delete and recreate the database tables
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                // Add user records to the User tables
                for (int i = 1; i <= countUsers; i++)
                {
                    dbContext.User.Add(new User { Name = "User" + i, Version = 1 });
                    dbContext.SaveChanges();
                }
                // Add category records to the Category table
                for (int i = 1; i <= countCategories; i++)
                {
                    dbContext.Category.Add(new Category { CategoryId = "Category" + i });
                    dbContext.SaveChanges();
                }
            }
        }

        // Method to get the appropriate connection string based on a category
        private string GetConnectionString(string category)
        {
            // Generate a hash based on the category
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.ASCII.GetBytes(category));

            // Calculate an index based on the hash to pick a connection string
            var x = BitConverter.ToUInt16(hash, 0) % _connectionStrings.Count;

            // Return the selected connection string
            return _connectionStrings[x];
        }
    }
}