# You've Found a Manifest-Aware Tool or Mod

## What does it mean to be Manifest-Aware?
Awarness of what?
*What is a manifest?*
A manifest tells apps and other mods about a manifested mod, including:
* Its name
* Who made it
* What version it is
* Where it can be downloaded
* What packs or other mods it requires
* The guidelines for properly installing it

So, if you've found a manifest-aware mod, then it can learn these kinds of details about other mods which contain manifests.
If you've found a manifest-aware tool, that means it understands manifests and can possibly modify them to suit the needs of mod creators.

## How do I manifest my own mods?
Right now, here's the list of manifest-aware tools of which we know that can help you add and maintain manifests in your mods:
* [PlumbBuddy](https://plumbbuddy.app) - A friendly helper for Sims 4 mods

We'll add more as we become aware of them, so check back later if you're interested.

Also, feel free to plop this fun sticker on your mods' download pages as you manifest them.
You can link players who use your custom content to [this page](MANIFESTED.md) so they can learn about all the benefits the manifest in your mod can bring them.

<img src="Manifested.png" width="180" height="60" />

Here's some example HTML for ya:
```html
<a href="https://github.com/Llama-Logic/LlamaLogic/blob/main/MANIFESTED.md"
   title="This mod has a manifest inside. Click here to learn more.">
   <img src="https://raw.githubusercontent.com/Llama-Logic/LlamaLogic/refs/heads/main/Manifested.png"
        width="180" height="60" />
</a>
```

If you're worried about ad blockers or can't write raw HTML on the platform hosting your mod's download page, you can just download the image above for yourself, put it on your own site in a way that works for you, and link to this page if you want to help folks out learning about manifests.

## Actually, I create mod tooling. How can I support manifests in my mod or tool?
How about that!
Well, to understand manifests themselves, it might help to review [this documentation](https://llama-logic.github.io/LlamaLogic/packages/LlamaLogic.Packages.Models.ModFileManifest.ModFileManifestModel.html).
If you're using .NET to make your tool, you can use [our open source NuGet package](https://www.nuget.org/packages/LlamaLogic.Packages) to easily read and modify manifests.
In addition to that, if you're making a manifest-aware mod, studying [this documentation](https://llama-logic.github.io/LlamaLogic/packages/LlamaLogic.Packages.Models.GlobalManifest.GlobalModsManifestModel.html) may be beneficial, as the global manifest snippet tuning can be *very helpful* in wrangling manifests from the context of a script mod at runtime.

Also, feel free to plop this fun sticker on your mod or tool's download pages as you add manifest awareness.
You can link others who use your mod or tool to this page so they can learn about all the benefits its manifest awareness can bring them.

<img src="Manifest-Aware.png" width="180" height="60" />

Here's some example HTML for ya:
```html
<a href="https://github.com/Llama-Logic/LlamaLogic/blob/main/MANIFEST-AWARE.md"
   title="This mod or tool is manifest-aware. Click here to learn more.">
   <img src="https://raw.githubusercontent.com/Llama-Logic/LlamaLogic/refs/heads/main/Manifest-Aware.png"
        width="180" height="60" />
</a>
```

If you're worried about ad blockers or can't write raw HTML on the platform hosting your mod or tool download page, you can just download the image above for yourself, put it on your own site in a way that works for you, and link to this page if you want to help folks out learning about manifests.
