using Blazor.Fluxor.UnitTests.SupportFiles;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Blazor.Fluxor.UnitTests.StoreTests
{
	public partial class StoreTests
	{
		public class AddFeature
		{
			[Fact]
			public async Task AddsFeatureToFeaturesDictionary()
			{
				const string featureName = "123";
				var mockFeature = new Mock<IFeature>();
				mockFeature
					.Setup(x => x.GetName())
					.Returns(featureName);

				var subject = await Store.Initialize(new TestStoreInitializer());
				await subject.AddFeature(mockFeature.Object);

				Assert.Same(mockFeature.Object, subject.Features[featureName]);
			}

			[Fact]
			public async Task ThrowsArgumentException_WhenFeatureWithSameNameAlreadyExists()
			{
				const string featureName = "1234";
				var mockFeature = new Mock<IFeature>();
				mockFeature
					.Setup(x => x.GetName())
					.Returns(featureName);

				var subject = await Store.Initialize(new TestStoreInitializer());
				await subject.AddFeature(mockFeature.Object);

				await Assert.ThrowsAsync<ArgumentException>(async () =>
				{
					await subject.AddFeature(mockFeature.Object);
				});
			}
		}
	}
}
