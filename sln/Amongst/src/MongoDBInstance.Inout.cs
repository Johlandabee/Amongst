﻿using System;
using System.Diagnostics;
using System.IO;
using Amongst.Exception;

namespace Amongst
{
    public partial class MongoDBInstance
    {
        /// <summary>
        /// Import data using native mongoimport functionality.
        /// This is tied to the current instance of mongod.
        /// </summary>
        /// <param name="database">Database name.</param>
        /// <param name="collection">Collection name.</param>
        /// <param name="filePath">File to import.</param>
        /// <param name="dropCollection">Drop existing collections?</param>
        /// <param name="timeout">Failure timeout. 5000ms by default.</param>
        public void Import(string database, string collection, string filePath, bool dropCollection = true,
            int timeout = 5000)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Could not import file {filePath}.");

            var fullPath = Path.Combine(_binaryPath, "mongoimport");

#if NETSTANDARD1_6
            if (IsUnix())
                SetExecutableBit(fullPath);
#endif
            // mongoimport wants a UNIX path.
            filePath = filePath.Replace("\\", "/");

            var drop = dropCollection ? "--drop" : null;
            var logVerbosity = _options.LogVerbosity == LogVerbosity.Quiet
                ? "--quiet"
                : _options.LogVerbosity == LogVerbosity.Verbose
                    ? "--verbose"
                    : null;

            var args = new[]
            {
                $"--host {_connection.IP}:{_connection.Port}",
                $"--db {database}",
                $"--collection {collection}",
                $"--file {filePath}",
                $"{drop}",
                $"{logVerbosity}"
            };

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            p.OutputDataReceived += OnOutputDataReceived;
            p.ErrorDataReceived += OnErrorDataReceived;

            p.Start();

            var exited = p.WaitForExit(timeout);
            if (!exited)
                throw new TimeoutException(
                    $"Mongoimport failed to import {filePath} to {database}/{collection} after {timeout} milliseconds.");

            if (p.ExitCode != 0)
                throw new ExitCodeException(
                    $"Mongoimport failed to import {filePath} to {database}/{collection}. Exit code {p.ExitCode}.");

            if (_options.LogVerbosity > LogVerbosity.Normal)
                _options.OutputHelper.WriteLine($"Successfully imported {filePath} to {database}/{collection}");
        }

        /// <summary>
        /// Export data using native mongoexport functionality.
        /// This is tied to the current instance of mongod.
        /// </summary>
        /// <param name="database">Database name.</param>
        /// <param name="collection">Collection name.</param>
        /// <param name="filePath">Export destination.</param>
        /// <param name="timeout">Failure timeout. 5000ms by default.</param>
        public void Export(string database, string collection, string filePath, int timeout = 5000)
        {
            var fullPath = Path.Combine(_binaryPath, "mongoexport");

#if NETSTANDARD1_6
            if (IsUnix())
                SetExecutableBit(fullPath);
#endif
            // mongoexpot wants a UNIX path.
            filePath = filePath.Replace("\\", "/");

            var logVerbosity = _options.LogVerbosity == LogVerbosity.Quiet
                ? "--quiet"
                : _options.LogVerbosity == LogVerbosity.Verbose
                    ? "--verbose"
                    : null;

            var args = new[]
            {
                $"--host {_connection.IP}:{_connection.Port}",
                $"--db {database}",
                $"--collection {collection}",
                $"--out {filePath}",
                $"{logVerbosity}"
            };

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            p.OutputDataReceived += OnOutputDataReceived;
            p.ErrorDataReceived += OnErrorDataReceived;

            p.Start();

            var exited = p.WaitForExit(timeout);
            if (!exited)
                throw new TimeoutException(
                    $"Mongoexport failed to export {database}/{collection} to {filePath} after {timeout} milliseconds.");

            if (p.ExitCode != 0)
                throw new ExitCodeException(
                    $"Mongoexport failed to export {database}/{collection} to {filePath}. Exit code {p.ExitCode}.");

            if (_options.LogVerbosity > LogVerbosity.Normal)
                _options.OutputHelper.WriteLine($"Successfully exported {database}/{collection} to {filePath}");
        }
    }
}
