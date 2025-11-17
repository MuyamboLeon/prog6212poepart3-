using The_CMCS.Models;
using System.Collections.Generic;

namespace The_CMCS.Services
{
    public interface IClaimsService
    {
        // Claim methods
        List<Claim> GetAllClaims();
        List<Claim> GetClaimsByUser(string userId);
        List<Claim> GetClaimsByDepartment(string department);
        Claim GetClaimById(string id);
        List<Claim> GetPendingClaims();
        bool AddClaim(Claim claim);
        bool UpdateClaim(Claim claim);
        bool DeleteClaim(string claimId);

        // Document methods
        List<SupportingDocument> GetDocumentsByClaimId(string claimId);
        SupportingDocument GetDocumentById(string id);
        bool AddDocument(SupportingDocument document);

        // User methods
        List<User> GetAllUsers();
        User GetUserByUsername(string username);
        User GetUserById(string id);
        bool AddUser(User user);
        bool UpdateUser(User user);
        bool DeleteUser(string userId);

        // Approval workflow methods
        bool ApproveByCoordinator(string claimId, string approvedBy);
        bool RejectByCoordinator(string claimId, string rejectedBy, string rejectionReason);
        bool ApproveByManager(string claimId, string approvedBy);
        bool RejectByManager(string claimId, string rejectedBy, string rejectionReason);
        List<Claim> GetClaimsPendingManagerReview();

        // Report methods
        List<Report> GenerateMonthlyReport(string month, int year);
        List<Report> GenerateDepartmentReport(string department);
        byte[] GenerateInvoicePdf(Claim claim);

        // Backward compatibility methods
        bool ApproveClaim(string claimId, string approvedBy);
        bool RejectClaim(string claimId, string rejectedBy, string rejectionReason);
        bool CreateClaim(Claim claim, List<IFormFile> supportingDocuments);
    }
}