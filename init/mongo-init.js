db = db.getSiblingDB('ConfigurationDB');

db.createCollection('ConfigurationItems');
db.ConfigurationItems.insertMany([
    { Name: "SiteName", Type: "String", Value: "Boyner.com.tr", IsActive: true, ApplicationName: "SERVICE-A" },
    { Name: "IsBasketEnabled", Type: "Boolean", Value: "1", IsActive: true, ApplicationName: "SERVICE-B" },
    { Name: "MaxItemCount", Type: "Integer", Value: "50", IsActive: false, ApplicationName: "SERVICE-A" },
]);