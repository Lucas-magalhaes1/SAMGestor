namespace SAMGestor.Domain.Enums
{
    [Flags]
    public enum RahaminVidaEdition
    {
        None = 0, 

        VidaI_2016_03_Cacador        = 1 << 0,
        VidaII_2016_10_Cacador       = 1 << 1,
        VidaIII_2017_01_Cacador      = 1 << 2,
        VidaIV_Juvenil_2017_09_Cacador = 1 << 3,
        VidaV_2018_02_Cacador        = 1 << 4,
        VidaVI_2018_10_Cacador       = 1 << 5,
        VidaVII_2019_02_Cacador      = 1 << 6,
        VidaVIII_2019_10_Cacador     = 1 << 7,
        VidaIX_2022_10_Cacador       = 1 << 8,
        VidaX_2023_02_Cacador        = 1 << 9,
        VidaXI_2023_10_Cacador       = 1 << 10,
        VidaXII_2024_02_Cacador      = 1 << 11,
        VidaXIII_2024_11_Cacador     = 1 << 12,
        VidaXIV_2025_02_Cacador      = 1 << 13
    }
}