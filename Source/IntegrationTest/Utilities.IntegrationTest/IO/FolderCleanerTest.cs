﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using pdfforge.PDFCreator.Utilities.IO;
using PDFCreator.TestUtilities;

namespace PDFCreator.Utilities.IntegrationTest.IO
{
    [TestFixture]
    internal class FolderCleanerTest
    {
        [SetUp]
        public void SetUp()
        {
            _tempFolder = TempFileHelper.CreateTempFolder("FolderCleanerTest");
        }

        [TearDown]
        public void TearDown()
        {
            TempFileHelper.CleanUp();
        }

        private string _tempFolder;

        private IList<string> CreateSubFolderWithSomeFiles(int numberOfFiles, TimeSpan age)
        {
            var folderName = Path.Combine(_tempFolder, Guid.NewGuid().ToString());
            Directory.CreateDirectory(folderName);
            var dir = new DirectoryInfo(folderName);
            dir.CreationTime = DateTime.Now - age;

            return CreateTempFiles(folderName, numberOfFiles, age);
        }

        private IList<string> CreateTempFiles(string folderName, int numberOfFiles, TimeSpan age)
        {
            var files = new List<string>();

            for (var i = 0; i < numberOfFiles; i++)
            {
                var guid = Guid.NewGuid().ToString();
                var filename = Path.Combine(folderName, $"{guid}.tmp");
                File.WriteAllText(filename, $"Temp file {guid}");
                var fileTime = DateTime.Now - age;
                File.SetCreationTime(filename, fileTime);
                Assert.AreEqual(fileTime, File.GetCreationTime(filename));
                files.Add(filename);
            }

            return files;
        }

        [Test]
        public void Clean_WithEmptyFolder_DoesNothing()
        {
            CreateTempFiles(_tempFolder, 6, TimeSpan.Zero);

            var cleaner = new FolderCleaner(_tempFolder);
            cleaner.Clean();

            Assert.IsTrue(Directory.Exists(_tempFolder), "The directory did not exist after cleaning up: " + _tempFolder);
        }

        [Test]
        public void Clean_WithNonexistingPath_DoesNothing()
        {
            Directory.Delete(_tempFolder);

            var cleaner = new FolderCleaner(@"C:\SomeFolderThatDoesnotExist\With\Some\Sub\Folder");
            cleaner.Clean();
        }

        [Test]
        public void Clean_WithSomeFiles_CleansAllFiles()
        {
            CreateTempFiles(_tempFolder, 4, TimeSpan.Zero);

            var cleaner = new FolderCleaner(_tempFolder);
            cleaner.Clean();

            Assert.IsTrue(Directory.Exists(_tempFolder), "The directory did not exist after cleaning up: " + _tempFolder);
            Assert.IsFalse(Directory.EnumerateFileSystemEntries(_tempFolder).Any(), "The directory is not empty after cleaning up: " + _tempFolder);
        }

        [Test]
        public void Clean_WithSomeFilesAndSubfolder_CleansAllFilesAndRemovesSubfolders()
        {
            CreateTempFiles(_tempFolder, 6, TimeSpan.Zero);
            var subFolder = Path.Combine(_tempFolder, "Subfolder");
            Directory.CreateDirectory(subFolder);
            CreateTempFiles(subFolder, 6, TimeSpan.Zero);

            var cleaner = new FolderCleaner(_tempFolder);
            cleaner.Clean();

            Assert.IsTrue(Directory.Exists(_tempFolder), "The directory did not exist after cleaning up: " + _tempFolder);
            Assert.IsFalse(Directory.EnumerateFileSystemEntries(_tempFolder).Any(), "The directory is not empty after cleaning up: " + _tempFolder);
        }

        [Test]
        public void CleanWithTimeSpan_WithSomeFiles_CleansAllFilesOlderThanTimeSpan()
        {
            CreateTempFiles(_tempFolder, 6, TimeSpan.FromDays(2));
            CreateSubFolderWithSomeFiles(6, TimeSpan.FromDays(2));
            var files = CreateSubFolderWithSomeFiles(1, TimeSpan.Zero);

            var cleaner = new FolderCleaner(_tempFolder);
            cleaner.Clean(TimeSpan.FromDays(1));

            Assert.IsTrue(Directory.Exists(_tempFolder), "The directory did not exist after cleaning up: " + _tempFolder);
            Assert.AreEqual(1, Directory.GetDirectories(_tempFolder).Count(), "There was more than one folder left");
            Assert.AreEqual(1, Directory.GetFiles(_tempFolder, "*", SearchOption.AllDirectories).Count(), "Found more than the one file after cleaning up");
            Assert.AreEqual(files.First(), Directory.GetFiles(_tempFolder, "*", SearchOption.AllDirectories).First());
        }
    }
}