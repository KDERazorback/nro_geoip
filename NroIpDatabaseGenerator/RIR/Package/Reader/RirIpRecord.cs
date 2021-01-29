using System;
using System.Collections.Generic;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package.Reader
{
    internal class RirIpRecord
    {
        public RirIpRecord(Alpha2.IsoAlpha2CountryCodes ccResolver, string countryIsoCode, string startAddress, long addressCount, DateTime assignDate, string statusString, string city = null)
        {
            CountryIsoCode = countryIsoCode;
            StartAddress = startAddress ?? throw new ArgumentNullException(nameof(startAddress));
            AddressCount = addressCount;
            AssignDate = assignDate;
            StatusString = statusString ?? throw new ArgumentNullException(nameof(statusString));
            CountryCodeResolver = ccResolver ?? throw new ArgumentNullException(nameof(ccResolver));
            City = city;

            RirIpRecordStatus status;
            if (Enum.TryParse<RirIpRecordStatus>(StatusString.Trim(), true, out status))
                Status = status;
            else
                Status = RirIpRecordStatus.Unknown;

            if (StartAddress.Contains('/'))
            {
                string[] segments = StartAddress.Split('/', StringSplitOptions.None);
                StartAddress = segments[0].Trim();
                MaskLength = int.Parse(segments[1].Trim());
            }
            else
            {
                if (addressCount < 1)
                    MaskLength = 32;
                else
                {
                    double mask = Math.Log2(addressCount);
                    if ((int)mask < mask)
                        MaskLength = 32 - (int)mask + 1;
                    else
                        MaskLength = 32 - (int)mask;
                }
            }

            string[] ipvalues = StartAddress.Split('.', StringSplitOptions.None);
            if (ipvalues.Length != 4)
                throw new ArgumentException("The provided IP Address is invalid.", nameof(StartAddress));

            uint ipdec = 0;
            ipdec += byte.Parse(ipvalues[0]);
            ipdec <<= 8;
            ipdec += byte.Parse(ipvalues[1]);
            ipdec <<= 8;
            ipdec += byte.Parse(ipvalues[2]);
            ipdec <<= 8;
            ipdec += byte.Parse(ipvalues[3]);

            AddressDec = ipdec;


            uint maskOverlay = 0xFFFFFFFF;
            maskOverlay <<= 32 - MaskLength;
            IsNetworkValid = (AddressDec & maskOverlay) == AddressDec;
        }

        public string CountryIsoCode { get; }
        public string StartAddress { get; }
        public long AddressCount { get; }
        public DateTime AssignDate { get; }
        public RirIpRecordStatus Status { get; }
        public string StatusString { get; }
        protected Alpha2.IsoAlpha2CountryCodes CountryCodeResolver { get; }
        public string City { get; }
        public string Country
        {
            get
            {
                return CountryCodeResolver.GetNameFromCode(CountryIsoCode);
            }
        }
        public string AddressCidr => string.Format("{0}/{1}", StartAddress, MaskLength);
        public uint AddressDec { get; }
        public int MaskLength { get; }
        public bool IsNetworkValid { get; }
    }
}
