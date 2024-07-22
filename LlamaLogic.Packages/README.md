<!-- TOC -->

- [How to install](#how-to-install)
  - [Using the .NET CLI](#using-the-net-cli)
  - [Using Visual Studio](#using-visual-studio)
- [Common tasks](#common-tasks)
  - [Creating a new package](#creating-a-new-package)
  - [Loading a package](#loading-a-package)
  - [Getting the keys of resources in a package](#getting-the-keys-of-resources-in-a-package)
  - [Getting the content of a resource in a package](#getting-the-content-of-a-resource-in-a-package)
  - [Adding or updating the content of a resource in a package](#adding-or-updating-the-content-of-a-resource-in-a-package)
  - [Saving a package](#saving-a-package)

<!-- /TOC -->
# How to install

## Using the .NET CLI
Navigate to your project in a terminal and enter the following command:
```bash
dotnet add package LlamaLogic.Packages
```

## Using Visual Studio
Follow these steps:
1. right-click on your project under **Solution Explorer**;
2. select **Manage NuGet Packages...**;
3. in the **Search** textbox, enter `LlamaLogic.Packages`;
4. press the return key;
5. select the **LlamaLogic.Packages** result; and,
6. click the **Install** button.

# Common tasks

## Creating a new package
Use the constructor of the `Package` class.
```csharp
using LlamaLogic.Packages;

var brandNewCareerMod = new Package();
```

## Loading a package
Pass a `Stream` object to `LlamaLogic.Packages.Package.FromStream` or `LlamaLogic.Packages.Package.FromStreamAsync` to be given a package object to examine and/or alter the contents of a package.
```csharp
using LlamaLogic.Packages;

using var packageStream = File.OpenRead(@"C:\Users\Jessica\Downloads\SnazzyCouch.package");
using var package = await Package.FromStreamAsync(packageStream);
```

***Important**: In order to conserve memory, `Package` objects do not load resources from streams until they are requested. It is important to create streams only for use by `Package` objects and to let them dispose of those streams when they, themselves, are disposed of by you.*

## Getting the keys of resources in a package
Call the `GetResourceKeys` method of a `Package` object to be given an `IEnumerable<LlamaLogic.Packages.PackageResourceKey>`.
```csharp
var keys = package.GetResourceKeys();
Console.WriteLine($"This package has {keys.Count:n0} resource(s) consisting of the following types: {string.Join(", ", keys.GroupBy(key => key.Type.ToString()).OrderBy(keysGroupedByType => keysGroupedByType.Key /* the type of which all keys in this group are a part */).Select(keysGroupedByType => $"{keysGroupedByType.Key} ({keysGroupedByType.Count():n0})"))}");
```

## Getting the content of a resource in a package
Call the `GetResourceContent` or `GetResourceContentAsync` method of a `Package` object with a `PackageResourceKey` to be given a `ReadOnlyMemory<byte>` consisting of the resource content.
```csharp
using System.Text;

var loot_Cauldron_Potion_FeelGood_Success_key =
    new PackageResourceKey
    (
        PackageResourceType.ActionTuning,
        0x15,
        0x346c3
    );
var actionTuningXml =
    await package.GetResourceContentAsync
    (loot_Cauldron_Potion_FeelGood_Success_key);
var actionTuningXmlString = Encoding.UTF8.GetString(actionTuningXml.Span);
Console.WriteLine("Potion of Plentiful Needs success override:");
Console.WriteLine(actionTuningXmlString);
```

*Note: Packages may contain content which has been compressed. The library abstracts this detail away by performing any decompression which may be needed when you request the content of a resource. You will always be given usable data that is not compacted or encrypted in any way.*

## Adding or updating the content of a resource in a package
Call the `SetResourceContent` method of a `Package` object with a `PackageResourceKey` and a `ReadOnlySpan<byte>` representing the content of the resource that you wish to add or update.
```csharp
using System.Linq;
using System.Xml.Linq;

// Kuttoe says you guys shouldn't be getting your jollies from a potion...
var actionTuningXmlDocument = XDocument.Parse(actionTuningXmlString);
var funHygieneAndSocialMotiveStatisticsLootActionsXPathQuery = "./I[@i = 'action' and @c = 'LootActions' and @m = 'interactions.utils.loot']/L[@n = 'loot_actions']/V[@t = 'statistics']/V[@n = 'statistics' and @t = 'statistic_set_max']/U[@n = 'statistic_set_max']/T[@n = 'stat' and normalize-space(text()) = ('16655', '16657', '16658')]/../../..";
var funHygieneAndSocialMotiveStatisticsLootActions =
    actionTuningXmlDocument.XPathSelectElements
    (funHygieneAndSocialMotiveStatisticsLootActionsQuery)
    .ToList().AsReadOnly();
foreach (var lootAction in funHygieneAndSocialMotiveStatisticsLootActions)
    lootAction.Remove();
package.SetResourceContent
(
    loot_Cauldron_Potion_FeelGood_Success_key,
    Encoding.UTF8.GetBytes(actionTuningXmlDocument.ToString())
);
```

## Saving a package
Call the `SaveTo` or `SaveToAsync` method with a `Stream` object to save current state of the `Package` object to that stream.
```csharp
using var savePackageStream = File.OpenWrite(@"C:\Users\Jessica\Desktop\SnazzyCouchRevised.package");
await package.SaveToAsync(savePackageStream);
```

***Important**: If the `Package` object was originally created from a stream, that stream is still in use. Do not pass the same stream to this method, and do not create a new stream writing to the same file. If you intend to save changes to the original file, have your `Package` object save to an alternate location (such as one provided by `System.IO.Path.GetTempFileName`), dispose of your `Package` object (causing it to dispose of its underlying original stream), and then overwrite the original file with the one created in the temporary location.*