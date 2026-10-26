#!/bin/zsh

rm -rf bin/ obj/ ../LlamaLogic.EpicTuningTime.Components/bin/ ../LlamaLogic.EpicTuningTime.Components/obj/
dotnet build -c Release
