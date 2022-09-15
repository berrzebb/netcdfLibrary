// See https://aka.ms/new-console-template for more information
using netCDFLibrary;
using netCDFLibrary.Data;
using OpenCvSharp;
CoordinateTransformatter transformatter = new CoordinateTransformatter();

using (NetCDFLib lib = new NetCDFLib("F:\\sds\\sampleData\\ERA5_2021.nc"))
{
    lib.ShowMetadata();
    foreach (var item in Enum.GetValues<ColormapTypes>())
    {
        HeatMapBuilder.generate_colorMap(item,lib["t2m"].Units, 0, 1, 256);
    }
    var times = lib.TimeIndex();
    {
        for (int time = 0; time < lib.Dimensions[0].Length; time++)
        {
            Dim dim = new Dim("time", new System.Range(time, time));
            NetCDFPrimitiveData data = lib["t2m"][
                Dim.Create("time", time..time),
                Dim.Create("latitude", ..),
                Dim.Create("longitude", ..)
            ];

            var palette = ColorPalette.Create(ColormapTypes.Viridis, -1, -1, 255);
            HeatMapBuilder.Generate(transformatter, ,palette, ref data, $"heatmap\\ERA5\\t2m\\{time}.png", isContour: true);

            NetCDFPrimitiveData UData = lib["u10"][Dim.Create("time", time..time),
                Dim.Create("latitude", ..),
                Dim.Create("longitude", ..)
            ];
            
            NetCDFPrimitiveData VData = lib["v10"][Dim.Create("time", time..time),
                Dim.Create("latitude", ..),
                Dim.Create("longitude", ..)
            ];

            palette = ColorPalette.Create(ColormapTypes.Viridis, -1, -1, 255);
            HeatMapBuilder.Generate(transformatter,palette, ref UData, ref VData, $"heatmap\\ERA5\\uv\\{time}.png", isContour: true);

        }
    }
}
using (NetCDFLib lib = new NetCDFLib("F:\\sds\\sampleData\\HYCOM2010_2021\\2021\\HYCOM_211231_210000.nc"))
{
    lib.ShowMetadata();
    var times = lib.TimeIndex();
    var depths = lib.Index<double>("depth");
    for (int time = 0; time < lib.Dimensions[0].Length; time++)
    {
        for (int depth = 0; depth < lib.Dimensions[1].Length; depth++)
        {
            var palette = ColorPalette.Create(ColormapTypes.Ocean, -1, -1, 255);
            NetCDFPrimitiveData data = lib["salinity"][
                Dim.Create("time", time..time),
                Dim.Create("depth", depth..depth),
                Dim.Create("lat", ..),
                Dim.Create("lon", ..)
            ];
            HeatMapBuilder.Generate(transformatter, palette, ref data, $"heatmap\\HYCOM\\salinity\\{time}_{depths[depth]}m.png", true, true);
            data = lib["water_temp"][
                Dim.Create("time", time..time),
                Dim.Create("depth", depth..depth),
                Dim.Create("lat", ..),
                Dim.Create("lon", ..)
            ];
            palette = ColorPalette.Create(ColormapTypes.Ocean, -1, -1, 255);
            HeatMapBuilder.Generate(transformatter, palette, ref data, $"heatmap\\HYCOM\\water_temp\\{time}_{depths[depth]}m.png", true, true);

            NetCDFPrimitiveData UData = lib["water_u"][
                Dim.Create("time", time..time),
                Dim.Create("depth", depth..depth),
                Dim.Create("lat", ..),
                Dim.Create("lon", ..)
            ];
            NetCDFPrimitiveData VData = lib["water_v"][
                Dim.Create("time", time..time),
                Dim.Create("depth", depth..depth),
                Dim.Create("lat", ..),
                Dim.Create("lon", ..)
            ];
            palette = ColorPalette.Create(ColormapTypes.Ocean, -1, -1, 255);

            HeatMapBuilder.Generate(transformatter, palette, ref UData, ref VData, $"heatmap\\HYCOM\\water_uv\\{time}_{depths[depth]}m.png", true);

        }
    }
}
