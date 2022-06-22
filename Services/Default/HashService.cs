using BCrypt.Net;
using System;

namespace RennerCoatingsAdminApi.Services {
    public static class HashService 
    {
        private const HashType hashType = HashType.SHA512;

        public static string CreateHash(this string input)
        {
            return BCrypt.Net.BCrypt.HashPassword(input, BCrypt.Net.BCrypt.GenerateSalt(), false, hashType: hashType).Trim().Replace(" ", "");
        }   

        public static void VerifyInput(this string input, string goodHash)
        {
            bool doesPasswordMatch = BCrypt.Net.BCrypt.Verify(input, goodHash, hashType: hashType);
            if (!doesPasswordMatch)
            {
                throw new Exception("Invalid input");
            }
        }
    }
}