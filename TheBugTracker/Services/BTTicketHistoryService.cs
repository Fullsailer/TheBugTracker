using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheBugTracker.Data;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services
{
    public class BTTicketHistoryService : IBTTicketHistoryService
    {
        private readonly ApplicationDbContext _context;
        
        public BTTicketHistoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task AddHistoryAsync(Ticket oldTicket, Ticket newTicket, string userId)
        {
            throw new NotImplementedException();
        }

        public Task<List<TicketHistory>> GetCompanyTicketsHistoriesAsync(int companyId)
        {
            throw new NotImplementedException();
        }

        public Task<List<TicketHistory>> GetProjectTicketsHistoriesAsync(int projectId, int companyId)
        {
            throw new NotImplementedException();
        }
    }
}
