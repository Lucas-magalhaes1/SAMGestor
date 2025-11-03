namespace SAMGestor.Domain.Enums
{
    [Flags]
    public enum RahaminAttempt
    {
        None = 0,

        RahaminPortaI_2015_EUA                  = 1 << 0,
        RahaminPortaII_2016_02_Cacador          = 1 << 1,
        RahaminPortaIII_2016_03_MorroDaFumaca   = 1 << 2,
        RahaminPortaIV_2016_08_Cacador          = 1 << 3,
        RahaminPortaV_Juvenil_2017_02_Cacador   = 1 << 4,
        RahaminPortaVI_2017_05_Cacador          = 1 << 5,
        RahaminPortaVII_2017_11_Cacador         = 1 << 6,
        RahaminPortaVIII_2018_06_Cacador        = 1 << 7,

        RahaminVidaI_2016_03_Cacador            = 1 << 8,
        RahaminVidaII_2016_10_Cacador           = 1 << 9,
        RahaminVidaIII_2017_01_Cacador          = 1 << 10,
        RahaminVidaIV_Juvenil_2017_09_Cacador   = 1 << 11,
        RahaminVidaV_2018_02_Cacador            = 1 << 12,
        RahaminVidaVI_2018_10_Cacador           = 1 << 13,
        RahaminVidaVII_2019_02_Cacador          = 1 << 14,
        RahaminVidaVIII_2019_10_Cacador         = 1 << 15,
        RahaminVidaIX_2022_10_Cacador           = 1 << 16,
        RahaminVidaX_2023_02_Cacador            = 1 << 17,
        RahaminVidaXI_2023_10_Cacador           = 1 << 18,
        RahaminVidaXII_2024_02_Cacador          = 1 << 19,
        RCristPortaI_2024_09_Cacador            = 1 << 20,
        RahaminVidaXIII_2024_11_Cacador         = 1 << 21,
        RahaminVidaXV_2025_02_Cacador           = 1 << 22
    }
}