# [Org Policy Customization](README.md#org-policy-customization)
- [Background](README.md#Background)
- [Overview](README.md#Overview)
- [Setting up org policy](README.md#setting-up-org-policy)
- [Modifying and customizing org policy](README.md#modifying-and-customizing-org-policy)
- [Common Application of Org Policy Customization](README.common-application-of-org-policy-customization)

## [Background](README.md#Background)

The Azure Tenant Security Solution (AzTS) can be used to obtain visibility to cloud subscriptions and resource configuration across multiple subscriptions in an enterprise environment. 
The AzTS is a logical progression of DevOps Kit which helps us move closer to an implementation of cloud security compliance solution using native security capabilities in Azure platform that are available today. Functionally, it is similar to running AzSK Continuous Assurance (CA) in central-scan mode.

You can know more about AzTS [here](https://github.com/azsk/AzTS-docs).

## [Overview](README.md#Overview)

Org Policy Customization provides capabilities to modify existing controls and add new controls (for existing services supported by AzTS) to customize the AzTS for your organization as per your need. 

This feature enhances AzTS Solution by enabling tenant security compliance owners to:
1) Modify existing controls
2) Add new controls
3) Scan the controls locally [Stand-alone Solution]

In this document, we will look at how to set up org policy, how to make modifications and additions to the controls, and how to accomplish various common org policy customization for the scanner.

## [Setting up org policy](README.md#setting-up-org-policy)

In this section, we will walk through the steps of setting up AzTS Scanner.

> **Note**: You would require at least 'Reader' level access on Subscription and 'Contributor' level access to the LA Workspace, Storage, etc.

Let's Start!

1. Clone [this](https://github.com/azsk/AzTS-Samples) GitHub repository in a new Visual Studio.
2. Go to AzTS_Extended folder and load the AzTS_Extended.sln.
3. Files to update: 
    * In local.settings.json file: 
        1. Application insights collect telemetry data from connected apps and provides Live Metrics, Log Analytics, etc. It has an instrumentation key which we need to configure into our function app i.e. APPINSIGHTS_INSTRUMENTATIONKEY and with this key app insights grab data from our app. Add instrumentation key for Application Insights by entering "APPINSIGHTS_INSTRUMENTATIONKEY"
	    2. Storage Account and Log Analytic Workspace are used to store the scan events, inventory, subscription scan progress details and results.
	        1. Add 'ResourceId' of the Storage Account (line 16),
		    2. Add 'WorkspaceId' and 'ResourceId' of the LA Workspace (line 33-34)
    * Mention the subscription to be scanned in Processor.cs, (line 33)
4. Build and Run

This will install the required [NuGet packages](https://www.nuget.org/packages/Microsoft.AzTS.Azure.Scanner/). It will import the dependencies and dynamic linked libraries of the AzTS Scanner to the user's solution along with the following templates:

| Template File | Description 
| ---- | ---- | 
| FeatureNameExt.json <br> [under the ControlConfigurationExt folder] | This file contains the setting of controls of a specific feature. A few meta-data are required for a control to be scanned which are mentioned in detail further ahead.
| FeatureNameControlEvaluatorExt.cs <br> [under the ControlEvaluator folder] | This file is used to override the base control evaluation method.

Next, we will look into how to modify an existing control or add a new control through this setup.

## [Modifying and customizing org policy](README.md#modifying-and-customizing-org-policy)

The typical workflow for all control changes will remain same and will involve the following basic steps:
1. Make modifications to the existing control metadata (Json files).
2. Add or Modify control methods in respective control evaluator files.
3. Build and Run

Steps to modify/add a control:
1. [**Control JSON:**](README.md#control-json)
    1. Copy _FeatureNameExt.json_ file and rename it accordingly. For example: StorageExt.json
	2. Fill the parameters according to the feature. For example: 
        ``` JSON
        {
            "FeatureName": "Storage",
            "Reference": "aka.ms/azsktcp/storage",
            "IsMaintenanceMode": false,
        }
        ```
	3. Add the control json with all parameters given in template. The following meta-data are required for a control to be scanned:
        ``` JSON
        "Controls": [
            {
            "ControlID": "",
            "Id": "",
            "Automated": "Yes",
            "MethodName": "",
            "DisplayName": "",
            "Enabled": false
            }
        ]
        ```
        > *Note*:  For **Id** above: If it is an existing control that you wish to modify, then use the same ID as used previously. If it is a new control, then follow a convention of a FeatureName followed by a *four* digit ID number. For example, "Storage1005" can be the ID for a new control implemented in Storage feature.

2. [**Control Evaluator:**](README.md#control-evaluator)
	1. Copy _FeatureNameControlEvaluatorExt.cs_ and rename it accordingly. For example: StorageControlEvaluatorExt.cs
	2. Change the FeatureNameEvaluatorExt and FeatureNameControlEvaluator according to the baseControlEvaluator name (line 13) as shown below.
        ``` CS
        // class FeatureNameEvaluatorExt : FeatureNameControlEvaluator
        class StorageEvaluatorExt : StorageControlEvaluator
        {
            // Add control methods here        
        }
        ```
    3. Add the control method according to the feature documentation.

