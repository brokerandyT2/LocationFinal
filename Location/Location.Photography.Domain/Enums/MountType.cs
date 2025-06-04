namespace Location.Photography.Domain.Enums
{
    public enum MountType
    {
        // Canon
        CanonEF = 1,
        CanonEFS = 2,
        CanonEFM = 3,
        CanonRF = 4,
        CanonFD = 5,

        // Nikon
        NikonF = 10,
        NikonZ = 11,
        Nikon1 = 12,

        // Sony
        SonyE = 20,
        SonyFE = 21,
        SonyA = 22,

        // Fujifilm
        FujifilmX = 30,
        FujifilmGFX = 31,

        // Pentax
        PentaxK = 40,
        PentaxQ = 41,

        // Micro Four Thirds
        MicroFourThirds = 50,

        // Leica
        LeicaM = 60,
        LeicaL = 61,
        LeicaSL = 62,
        LeicaTL = 63,

        // Olympus
        OlympusFourThirds = 70,

        // Panasonic
        PanasonicL = 80,

        // Sigma
        SigmaSA = 90,

        // Tamron
        TamronAdaptall = 100,

        // Generic/Other
        C = 200,
        CS = 201,
        M42 = 202,
        T2 = 203,
        Other = 999
    }
}