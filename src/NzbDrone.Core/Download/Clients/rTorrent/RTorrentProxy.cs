﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Extensions;
using CookComputing.XmlRpc;

namespace NzbDrone.Core.Download.Clients.RTorrent
{
    public interface IRTorrentProxy
    {
        string GetVersion(RTorrentSettings settings);
        List<RTorrentTorrent> GetTorrents(RTorrentSettings settings);

        void AddTorrentFromUrl(string torrentUrl, RTorrentSettings settings);
        void AddTorrentFromFile(string fileName, byte[] fileContent, RTorrentSettings settings);
        void RemoveTorrent(string hash, RTorrentSettings settings);
        void SetTorrentPriority(string hash, RTorrentSettings settings, RTorrentPriority priority);
        void SetTorrentLabel(string hash, string label, RTorrentSettings settings);
        bool HasHashTorrent(string hash, RTorrentSettings settings);
    }

    public interface IRTorrent : IXmlRpcProxy
    {
        [XmlRpcMethod("d.multicall2")]
        object[] TorrentMulticall(params string[] parameters);

        [XmlRpcMethod("load.start")]
        int LoadURL(string target, string data);

        [XmlRpcMethod("load.raw_start")]
        int LoadBinary(string target, byte[] data);

        [XmlRpcMethod("d.erase")]
        int Remove(string hash);

        [XmlRpcMethod("d.custom1.set")]
        string SetLabel(string hash, string label);

        [XmlRpcMethod("d.priority.set")]
        int SetPriority(string hash, long priority);

        [XmlRpcMethod("d.name")]
        string GetName(string hash);

        [XmlRpcMethod("system.client_version")]
        string GetVersion();
    }

    public class RTorrentProxy : IRTorrentProxy
    {
        private readonly Logger _logger;

        public RTorrentProxy(Logger logger)
        {
            _logger = logger;
        }

        public string GetVersion(RTorrentSettings settings)
        {
            _logger.Debug("Executing remote method: system.client_version");

            var client = BuildClient(settings);

            var version = client.GetVersion();

            return version;
        }

        public List<RTorrentTorrent> GetTorrents(RTorrentSettings settings)
        {
            _logger.Debug("Executing remote method: d.multicall2");

            var client = BuildClient(settings);
            var ret = client.TorrentMulticall("", "",
                "d.name=", // string
                "d.hash=", // string
                "d.base_path=", // string
                "d.custom1=", // string (label)
                "d.size_bytes=", // long
                "d.left_bytes=", // long
                "d.down.rate=", // long (in bytes / s)
                "d.ratio=", // long
                "d.is_open=", // long
                "d.is_active=", // long
                "d.complete="); //long

            var items = new List<RTorrentTorrent>();
            foreach (object[] torrent in ret)
            {
                var item = new RTorrentTorrent();
                item.Name = (string) torrent[0];
                item.Hash = (string) torrent[1];
                item.Path = (string) torrent[2];
                item.Category = (string) torrent[3];
                item.TotalSize = (long) torrent[4];
                item.RemainingSize = (long) torrent[5];
                item.DownRate = (long) torrent[6];
                item.Ratio = (long) torrent[7];
                item.IsOpen = Convert.ToBoolean((long) torrent[8]);
                item.IsActive = Convert.ToBoolean((long) torrent[9]);
                item.IsFinished = Convert.ToBoolean((long) torrent[10]);

                items.Add(item);
            }

            return items;
        }

        public bool HasHashTorrent(string hash, RTorrentSettings settings)
        {
            _logger.Debug("Executing remote method: d.name");

            var client = BuildClient(settings);

            try
            {
                var name = client.GetName(hash);
                if (name.IsNullOrWhiteSpace()) return false;
                bool metaTorrent = name == (hash + ".meta");
                return !metaTorrent;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void AddTorrentFromUrl(string torrentUrl, RTorrentSettings settings)
        {
            _logger.Debug("Executing remote method: load.start");

            var client = BuildClient(settings);

            var response = client.LoadURL("", torrentUrl);
            if (response != 0)
            {
                throw new DownloadClientException("Could not add torrent: {0}.", torrentUrl);
            }
        }

        public void AddTorrentFromFile(string fileName, Byte[] fileContent, RTorrentSettings settings)
        {
            _logger.Debug("Executing remote method: load.raw_start");

            var client = BuildClient(settings);

            var response = client.LoadBinary("", fileContent);
            if (response != 0)
            {
                throw new DownloadClientException("Could not add torrent: {0}.", fileName);
            }
        }

        public void RemoveTorrent(string hash, RTorrentSettings settings)
        {
            _logger.Debug("Executing remote method: d.erase");

            var client = BuildClient(settings);

            var response = client.Remove(hash);
            if (response != 0)
            {
                throw new DownloadClientException("Could not remove torrent: {0}.", hash);
            }
        }

        public void SetTorrentPriority(string hash, RTorrentSettings settings, RTorrentPriority priority)
        {
            _logger.Debug("Executing remote method: d.priority.set");

            var client = BuildClient(settings);

            var response = client.SetPriority(hash, (long) priority);
            if (response != 0)
            {
                throw new DownloadClientException("Could not set priority on torrent: {0}.", hash);
            }
        }

        public void SetTorrentLabel(string hash, string label, RTorrentSettings settings)
        {
            _logger.Debug("Executing remote method: d.set_custom1");

            var client = BuildClient(settings);

            var satLabel = client.SetLabel(hash, label);
            if (satLabel != label)
            {
                throw new DownloadClientException("Could set label on torrent: {0}.", hash);
            }
        }

        private IRTorrent BuildClient(RTorrentSettings settings)
        {
            var client = XmlRpcProxyGen.Create<IRTorrent>();

            client.Url = string.Format(@"{0}://{1}:{2}/{3}",
                                    settings.UseSsl ? "https" : "http",
                                    settings.Host,
                                    settings.Port,
                                    settings.UrlBase);

            client.EnableCompression = true;

            if (!settings.Username.IsNullOrWhiteSpace())
            {
                client.Credentials = new NetworkCredential(settings.Username, settings.Password);
            }

            return client;
        }
    }
}
