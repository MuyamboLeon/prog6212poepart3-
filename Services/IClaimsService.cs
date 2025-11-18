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

        // Report methods (Legacy - for backward compatibility)
        List<Report> GenerateMonthlyReport(string month, int year);
        List<Report> GenerateDepartmentReport(string department);
        byte[] GenerateInvoicePdf(Claim claim);

        // Backward compatibility methods
        bool ApproveClaim(string claimId, string approvedBy);
        bool RejectClaim(string claimId, string rejectedBy, string rejectionReason);
        bool CreateClaim(Claim claim, List<IFormFile> supportingDocuments);

        // Automation methods for Lecturer
        Claim AutoCalculateClaim(Claim claim);
        (bool isValid, string errors) ValidateClaimSubmission(Claim claim);

        // Automation methods for Coordinator
        List<Claim> GetClaimsForAutoVerification();
        (bool isValid, string policyCheck) VerifyClaimAgainstPolicies(Claim claim);
        bool AutoApproveClaims(List<string> claimIds, string approvedBy);

        // Automation methods for Manager
        List<Claim> GetClaimsForAutoApproval();
        bool AutoApproveByManager(List<string> claimIds, string approvedBy);

        // Automation methods for HR (Renamed to avoid conflicts)
        AutomatedReport GenerateAutomatedMonthlyReport(string month, int year);
        AutomatedReport GenerateAutomatedDepartmentReport(string department);
        byte[] GenerateBulkInvoicesPdf(List<string> claimIds);

        // Validation Rules Management
        List<ClaimValidationRule> GetValidationRules();
        bool UpdateValidationRule(ClaimValidationRule rule);

        // Report Management
        List<AutomatedReport> GetGeneratedReports();
        AutomatedReport GetReportById(string id);

        // Additional utility methods
        List<byte[]> GenerateBulkInvoices(List<string> claimIds);
        Dictionary<string, object> GetSystemOverview();
        List<Report> GetReports();
    }
}