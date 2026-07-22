using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Infrastructure.Identity
{
    public class DevTrackIdentityDbContext : IdentityDbContext<ApplicationUser>
    {
        public DevTrackIdentityDbContext(DbContextOptions<DevTrackIdentityDbContext> options)
            : base(options)
        {
        }
    }
}
