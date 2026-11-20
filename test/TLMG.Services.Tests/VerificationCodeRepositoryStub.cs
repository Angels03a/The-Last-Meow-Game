using System;
using System.Collections.Generic;

using TLMG.Domain.Repositories;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Local double of IVerificationCodeRepository. It records what the code store sends and returns the results
    /// the test sets; it never decides whether a code is valid, because that rule lives in the database.
    /// </summary>
    internal sealed class VerificationCodeRepositoryStub : IVerificationCodeRepository
    {
        internal bool SaveCodeSucceeds { get; set; } = true;

        internal int? PendingIdToReturn { get; set; }

        internal PendingRegistrationRecord PendingRegistrationToReturn { get; set; }

        internal VerificationCodeValidationResult ValidationResultToReturn { get; set; } =
            new VerificationCodeValidationResult { Status = VerificationCodeStatus.Valid };

        /// <summary>Codes received by SaveCode, in call order.</summary>
        internal List<VerificationCodeRecord> SavedCodes { get; } = new List<VerificationCodeRecord>();

        /// <summary>Codes received by ValidateCode, in call order.</summary>
        internal List<VerificationCodeRecord> ValidatedCodes { get; } = new List<VerificationCodeRecord>();

        /// <summary>Clock values received by ValidateCode, in call order.</summary>
        internal List<DateTime> ValidationTimesUtc { get; } = new List<DateTime>();

        /// <summary>Pending registrations received by SavePendingRegistration, in call order.</summary>
        internal List<PendingRegistrationRecord> SavedPendingRegistrations { get; } =
            new List<PendingRegistrationRecord>();

        /// <summary>Identifiers received by DeletePendingRegistration, in call order.</summary>
        internal List<int> DeletedPendingIds { get; } = new List<int>();

        /// <inheritdoc />
        public int? SavePendingRegistration(PendingRegistrationRecord pendingRegistration)
        {
            SavedPendingRegistrations.Add(pendingRegistration);

            return PendingIdToReturn;
        }

        /// <inheritdoc />
        public PendingRegistrationRecord FindPendingRegistrationByEmail(string email)
        {
            return PendingRegistrationToReturn;
        }

        /// <inheritdoc />
        public void DeletePendingRegistration(int pendingId)
        {
            DeletedPendingIds.Add(pendingId);
        }

        /// <inheritdoc />
        public bool SaveCode(VerificationCodeRecord verificationCodeRecord)
        {
            SavedCodes.Add(verificationCodeRecord);

            return SaveCodeSucceeds;
        }

        /// <inheritdoc />
        public VerificationCodeValidationResult ValidateCode(VerificationCodeRecord verificationCode, DateTime nowUtc)
        {
            ValidatedCodes.Add(verificationCode);
            ValidationTimesUtc.Add(nowUtc);

            return ValidationResultToReturn;
        }
    }
}
