﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Picton.Managers;
using Shouldly;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Picton.UnitTests.Managers
{
	public class BlobManagerTests
	{
		[Fact]
		public void Null_queueName_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = Misc.GetMockStorageAccount(null, null);
				var blobManager = new BlobManager(null, storageAccount.Object);
			});
		}

		[Fact]
		public void Empty_queueName_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = Misc.GetMockStorageAccount(null, null);
				var blobManager = new BlobManager("", storageAccount.Object);
			});
		}

		[Fact]
		public void Blank_queueName_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = Misc.GetMockStorageAccount(null, null);
				var blobManager = new BlobManager(" ", storageAccount.Object);
			});
		}

		[Fact]
		public void Null_IStorageAccount_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = (CloudStorageAccount)null;
				var blobManager = new BlobManager("mycontainer", storageAccount);
			});
		}

		[Fact]
		public void Null_storageAccount_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = (CloudStorageAccount)null;
				var blobManager = new BlobManager("mycontainer", storageAccount);
			});
		}

		[Fact]
		public void Initialization()
		{
			// Arrange
			var containerName = "mycontainer";
			var mockBlobContainer = Misc.GetMockBlobContainer(containerName);
			var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
			var storageAccount = Misc.GetMockStorageAccount(mockBlobClient, null);

			// Act
			var blobManager = new BlobManager(containerName, storageAccount.Object);

			// Assert
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		public void GetBlobContentAsync_blob_does_not_exist()
		{
			// Arrange
			var containerName = "mycontainer";
			var mockBlobContainer = Misc.GetMockBlobContainer(containerName);
			var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
			var storageAccount = Misc.GetMockStorageAccount(mockBlobClient, null);
			var blobName = "myblob.txt";
			var mockBlobUri = new Uri(Misc.BLOB_STORAGE_URL + blobName);

			var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, mockBlobUri);
			mockBlob
				.Setup(b => b.ExistsAsync(It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(false)
				.Verifiable();

			mockBlobContainer
				.Setup(c => c.GetBlobReference(blobName))
				.Returns(mockBlob.Object)
				.Verifiable();

			// Act
			var blobManager = new BlobManager(containerName, storageAccount.Object);
			var result = blobManager.GetBlobContentAsync(blobName, CancellationToken.None);
			result.Wait();
			var content = result.Result;

			// Assert
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			mockBlob.Verify();
			content.ShouldBeNull();
		}

		[Fact]
		public void GetBlobContentAsync_blob_exists()
		{
			// Arrange
			var containerName = "mycontainer";
			var mockBlobContainer = Misc.GetMockBlobContainer(containerName);
			var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
			var storageAccount = Misc.GetMockStorageAccount(mockBlobClient, null);
			var blobName = "myblob.txt";
			var mockBlobUri = new Uri(Misc.BLOB_STORAGE_URL + blobName);
			var expected = "Hello World!";

			var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, mockBlobUri);
			mockBlob
				.Setup(b => b.ExistsAsync(It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(true)
				.Verifiable();
			mockBlob
				.Setup(b => b.DownloadToStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Callback(async (Stream s, AccessCondition ac, BlobRequestOptions o, OperationContext oc, CancellationToken ct) =>
				{
					var buffer = expected.ToBytes();
					await s.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
				})
				.Verifiable();

			mockBlobContainer
				.Setup(c => c.GetBlobReference(blobName))
				.Returns(mockBlob.Object)
				.Verifiable();

			// Act
			var blobManager = new BlobManager(containerName, storageAccount.Object);
			var result = Encoding.UTF8.GetString(blobManager.GetBlobContentAsync(blobName).Result);

			// Assert
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			mockBlob.Verify();
			result.ShouldBe(expected);
		}
	}
}
