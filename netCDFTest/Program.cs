// See https://aka.ms/new-console-template for more information
using netCDFLibrary;
using netCDFLibrary.Data;
using OpenCvSharp;
using (NetCDFLib lib = new NetCDFLib("F:\\sds\\sampleData\\ERA5_2021.nc"))
{
    lib.ShowMetadata();
    var times = lib.TimeIndex();
    {
        for (int time = 0; time < lib.Dimensions[0].Length; time++)
        {
            NetCDFPrimitiveData data = lib["t2m"][null,
                Dim.Create(("time",time..time), ("latitude", ..),("longitude", ..))
            ];

            var palette = ColorPalette.Create(new ColorPaletteOptions(ColormapTypes.Viridis, -1, -1, 255));
            HeatMapBuilder.Generate(palette, ref data, $"heatmap\\ERA5\\t2m\\{time}.png");

            NetCDFPrimitiveData UData = lib["u10"][null, Dim.Create(
                ("time", time..time),
                ("latitude", ..),
                ("longitude", ..))
            ];
            
            NetCDFPrimitiveData VData = lib["v10"][null, Dim.Create(
                ("time", time..time),
                ("latitude", ..),
                ("longitude", ..))
            ];

            palette = ColorPalette.Create(new ColorPaletteOptions(ColormapTypes.Viridis, -1, -1, 255));
            //HeatMapBuilder.Generate(palette,UData, ref VData, $"heatmap\\ERA5\\uv\\{time}.png", new HeatMapOptions());

        }
    }
}
using (NetCDFLib lib = new NetCDFLib("F:\\sds\\sampleData\\HYCOM2010_2021\\2021\\HYCOM_211231_210000.nc"))
{
    lib.ShowMetadata();
    var times = lib.TimeIndex();
    var depths = lib.Index("depth");
    for (int time = 0; time < lib.Dimensions[0].Length; time++)
    {
        for (int depth = 0; depth < lib.Dimensions[1].Length; depth++)
        {
            var palette = ColorPalette.Create(new ColorPaletteOptions(ColormapTypes.Ocean, -1, -1, 255));
            NetCDFPrimitiveData data = lib["salinity"][null,
                Dim.Create(
                    ("time", time..time),
                    ("depth", depth..depth),
                    ("lat", ..),
                    ("lon", ..))
            ];
            HeatMapBuilder.Generate(palette, ref data, $"heatmap\\HYCOM\\salinity\\{time}_{depths[depth]}m.png");
            data = lib["water_temp"][null,
                Dim.Create(
                    ("time", time..time),
                    ("depth", depth..depth),
                    ("lat", ..),
                    ("lon", ..))
            ];
            palette = ColorPalette.Create(new ColorPaletteOptions(ColormapTypes.Ocean, -1, -1, 255));
            HeatMapBuilder.Generate(palette, ref data, $"heatmap\\HYCOM\\water_temp\\{time}_{depths[depth]}m.png");

            NetCDFPrimitiveData UData = lib["water_u"][null,
                Dim.Create(
                    ("time", time..time),
                    ("depth", depth..depth),
                    ("lat", ..),
                    ("lon", ..))];
            NetCDFPrimitiveData VData = lib["water_v"][null,
                Dim.Create(
                    ("time", time..time),
                    ("depth", depth..depth),
                    ("lat", ..),
                    ("lon", ..))
            ];
            palette = ColorPalette.Create(new ColorPaletteOptions(ColormapTypes.Ocean, -1, -1, 255));

            //HeatMapBuilder.Generate(palette, ref UData, ref VData, $"heatmap\\HYCOM\\water_uv\\{time}_{depths[depth]}m.png", true);

        }
    }
}
