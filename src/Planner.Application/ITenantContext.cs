using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planner.Application;

public interface ITenantContext {
    Guid TenantId { get; }
}

public sealed class StaticTenantContext : ITenantContext {
    public Guid TenantId => Guid.Parse("00000000-0000-0000-0000-000000000001");
}
