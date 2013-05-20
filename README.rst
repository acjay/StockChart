StockChart Design & Work Plan
=============================

Below is the work plan for the StockChart application.

Purpose
-------

Use C# and SciChart to create an application for visualizing stock prices vs. the DJIA.

Design
------

The application will be a single window application. The central element will be the stock price visualization.

Requirements:
 -  User can select a dataset in a given format.
 -  The dataset will be visualized in the chart.
 -  The user can select whether to view unaveraged data or a moving average.
 -  The user can opt to display the Dow Jones Industrial Average (DJIA) against their chosen data.

Work Plan
---------

#.  Set up development environment. (4 hours)

    -  install Windows image in Parallels
    -  setup VS2010 
    -  install Git
    
#.  Create application with dummy data to test SciChart. (2 hours)
    
    -  install SciChart
    -  create project
    -  check code into GitHub
    -  research sample application
    
#.  Modify application to display stock data. (2 hours)
    
    -  research stock chart functionality
    -  obtain or create test data
    -  modify application
    
#.  Add capability to load external data for display. (3 hours)

    -  research data loading
    -  add UI code for file loading
    -  obtain real data
    -  implement ability to change initial chart display
    
#.  Implement moving average filters. (1 hour)

    -  add UI code for filter size selection
    
#.  Add DJIA data. (1 hour)

    -  obtain data
    -  extend interface