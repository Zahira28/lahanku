using System;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Lahanku.ViewModels;

namespace Lahanku.Views.Modals
{
    public partial class MapPickerModalView : UserControl
    {
        private MapPickerModalViewModel? _viewModel;
        private bool _isWebViewInitialized;

        public MapPickerModalView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.MoveMapRequested -= OnMoveMapRequested;
            }

            if (e.NewValue is MapPickerModalViewModel vm)
            {
                _viewModel = vm;
                _viewModel.MoveMapRequested += OnMoveMapRequested;
                _ = InitializeMapAsync();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && !_isWebViewInitialized)
            {
                _ = InitializeMapAsync();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.MoveMapRequested -= OnMoveMapRequested;
            }
        }

        private async void OnMoveMapRequested(double lat, double lon, int zoom)
        {
            if (!_isWebViewInitialized || MapWebView.CoreWebView2 == null)
                return;

            try
            {
                var latStr = lat.ToString("0.000000", CultureInfo.InvariantCulture);
                var lonStr = lon.ToString("0.000000", CultureInfo.InvariantCulture);
                await MapWebView.ExecuteScriptAsync($"if (window.moveTo) {{ window.moveTo({latStr}, {lonStr}, {zoom}); }}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPicker] Error executing script: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task InitializeMapAsync()
        {
            if (_isWebViewInitialized || _viewModel == null)
                return;

            try
            {
                await MapWebView.EnsureCoreWebView2Async();
                _isWebViewInitialized = true;

                MapWebView.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 LahanKu/1.0";
                MapWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                var initialLat = _viewModel.InitialLatitude.ToString("0.000000", CultureInfo.InvariantCulture);
                var initialLon = _viewModel.InitialLongitude.ToString("0.000000", CultureInfo.InvariantCulture);
                var initialZoom = _viewModel.InitialZoom;

                var html = GenerateLeafletHtml(initialLat, initialLon, initialZoom);
                MapWebView.NavigateToString(html);

                MapLoadingOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPicker] WebView2 initialization error: {ex.Message}");
                // Jika webview2 gagal (misal runtime belum terinstal), sembunyikan overlay agar tidak hang
                MapLoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void OnWebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var rawJson = e.TryGetWebMessageAsString();
                if (string.IsNullOrWhiteSpace(rawJson))
                    return;

                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "coords_changed")
                {
                    if (root.TryGetProperty("lat", out var latProp) && root.TryGetProperty("lng", out var lngProp))
                    {
                        var lat = latProp.GetDouble();
                        var lng = lngProp.GetDouble();

                        Dispatcher.Invoke(() =>
                        {
                            _viewModel?.UpdateSelectedCoordinatesFromMap(lat, lng);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPicker] Error processing web message: {ex.Message}");
            }
        }

        private static string GenerateLeafletHtml(string lat, string lon, int zoom)
        {
            var cartoKey = Lahanku.Helpers.MapConfig.CartoApiKey;
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <link rel=""stylesheet"" href=""https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"" />
    <script src=""https://unpkg.com/leaflet@1.9.4/dist/leaflet.js""></script>
    <style>
        html, body, #map {{
            height: 100%;
            width: 100%;
            margin: 0;
            padding: 0;
            background: #f8fafc;
            font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif;
        }}
        .custom-pin {{
            display: flex;
            align-items: center;
            justify-content: center;
            filter: drop-shadow(0 4px 8px rgba(0,0,0,0.35));
            transition: transform 0.2s cubic-bezier(0.34, 1.56, 0.64, 1);
        }}
        .custom-pin:hover {{
            transform: scale(1.15);
        }}
        .leaflet-control-attribution {{
            font-size: 10px;
            background: rgba(255,255,255,0.85) !important;
            padding: 2px 6px !important;
            border-radius: 4px;
        }}
        .leaflet-control-layers {{
            border-radius: 10px !important;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15) !important;
            font-weight: 600;
            font-size: 12px;
            padding: 4px 8px !important;
        }}
    </style>
</head>
<body>
    <div id=""map""></div>
    <script>
        var initialLat = {lat};
        var initialLon = {lon};
        var initialZoom = {zoom};

        var map = L.map('map', {{
            center: [initialLat, initialLon],
            zoom: initialZoom,
            zoomControl: true
        }});

        // Layer CartoDB Voyager (Resmi dengan CARTO Basemaps API Key dari MapConfig)
        var streetLayer = L.tileLayer('https://{{s}}.basemaps.cartocdn.com/rastertiles/voyager/{{z}}/{{x}}/{{y}}{{r}}.png?key={cartoKey}', {{
            attribution: '&copy; <a href=""https://www.openstreetmap.org/copyright"">OpenStreetMap</a> &copy; <a href=""https://carto.com/attributions"">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 20
        }});

        // Layer Satelit Esri (Foto udara lahan/kebun nyata)
        var satelliteLayer = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{{z}}/{{y}}/{{x}}', {{
            attribution: 'Tiles &copy; Esri World Imagery',
            maxZoom: 19
        }});

        streetLayer.addTo(map);

        var baseLayers = {{
            ""🗺️ Peta Jalan"": streetLayer,
            ""🛰️ Foto Satelit"": satelliteLayer
        }};

        L.control.layers(baseLayers, null, {{ position: 'topright' }}).addTo(map);

        var pinSvg = '<svg width=""32"" height=""42"" viewBox=""0 0 24 32"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">' +
            '<path d=""M12 0C5.373 0 0 5.373 0 12c0 9 12 20 12 20s12-11 12-20c0-6.627-5.373-12-12-12z"" fill=""#059669""/>' +
            '<circle cx=""12"" cy=""11"" r=""5"" fill=""#FFFFFF""/>' +
            '</svg>';

        var pinIcon = L.divIcon({{
            className: 'custom-pin',
            html: pinSvg,
            iconSize: [32, 42],
            iconAnchor: [16, 42]
        }});

        var marker = L.marker([initialLat, initialLon], {{ icon: pinIcon, draggable: true }}).addTo(map);

        function notifyCoords(lat, lng) {{
            if (window.chrome && window.chrome.webview) {{
                window.chrome.webview.postMessage(JSON.stringify({{
                    type: 'coords_changed',
                    lat: lat,
                    lng: lng
                }}));
            }}
        }}

        map.on('click', function(e) {{
            marker.setLatLng(e.latlng);
            notifyCoords(e.latlng.lat, e.latlng.lng);
        }});

        marker.on('dragend', function(e) {{
            var pos = marker.getLatLng();
            notifyCoords(pos.lat, pos.lng);
        }});

        window.moveTo = function(lat, lng, zoom) {{
            map.flyTo([lat, lng], zoom || 15, {{ duration: 1.2 }});
            marker.setLatLng([lat, lng]);
        }};
    </script>
</body>
</html>";
        }
    }
}
